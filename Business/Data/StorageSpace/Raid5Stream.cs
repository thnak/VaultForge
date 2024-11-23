using System.Buffers;
using Business.Data.StorageSpace.Utils;

namespace Business.Data.StorageSpace;

public class Raid5Stream : Stream
{
    private long _originalSize;
    private readonly int _stripeSize;
    private long _position;
    private long StripeBlockIndex { get; set; }
    private int StripeRowIndex { get; set; }
    private int ActualBufferSize { get; set; }
    private int StartPaddingSize { get; set; }
    private static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;
    private static readonly ArrayPool<int> IntArrayPool = ArrayPool<int>.Shared;
    private readonly byte[][] _readPooledArrays;
    private readonly byte[][] _parityPoolBuffers;
    private readonly int[] _indicesArrayPool;
    public List<FileStream?> FileStreams { get; set; }

    public Raid5Stream(IEnumerable<string> path, long originalSize, int stripeSize, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
    {
        FileStreams = path.OpenFile(fileMode, fileAccess, fileShare, bufferSize: 4 * stripeSize);
        // FileStreams = path.Select(_ => new MemoryStream()).ToList()!;
        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
        ActualBufferSize = _stripeSize * (FileStreams.Count - 1);
        _readPooledArrays = new byte[FileStreams.Count][];
        _parityPoolBuffers = new byte[FileStreams.Count - 1][];
        _indicesArrayPool = IntArrayPool.Rent(1);
        for (int i = 0; i < FileStreams.Count; i++)
        {
            _readPooledArrays[i] = ByteArrayPool.Rent(stripeSize); // Rent an array large enough for `count`
        }

        for (int i = 0; i < FileStreams.Count - 1; i++)
        {
            _parityPoolBuffers[i] = ByteArrayPool.Rent(stripeSize); // Rent an array large enough for `count`
        }
    }

    public override bool CanSeek => true;
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override long Length => _originalSize;

    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPosition;

        // Calculate the new position based on the SeekOrigin
        switch (origin)
        {
            case SeekOrigin.Begin:
                newPosition = offset;
                break;
            case SeekOrigin.Current:
                newPosition = _position + offset;
                break;
            case SeekOrigin.End:
                newPosition = _originalSize + offset;
                break;
            default:
                throw new ArgumentException(@"Invalid SeekOrigin", nameof(origin));
        }

        // Validate that the new position is within the bounds of the file
        if (newPosition < 0 || newPosition > _originalSize)
            throw new ArgumentOutOfRangeException(nameof(offset), @"Seek position is out of range.");

        int availableNumDisks = FileStreams.Count - 1;

        StripeRowIndex = (int)(newPosition / availableNumDisks / _stripeSize);

        StripeBlockIndex = newPosition / _stripeSize;
        if (StripeBlockIndex > 1)
        {
            if (StripeBlockIndex % availableNumDisks != 0)
            {
                StripeBlockIndex -= 1;
            }
        }
        else
        {
            StripeBlockIndex = 0;
        }

        // Seek each file stream to the start of the stripe
        var seekPosition = StripeRowIndex * _stripeSize;

        FileStreams.Seek(seekPosition, SeekOrigin.Begin);

        // Adjust the read pointers to account for the position within the stripe
        _position = newPosition;
        StartPaddingSize = (int)(newPosition % (ActualBufferSize * (StripeRowIndex + 1)));

        return _position;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = Read(buffer, 0, bufferSize)) > 0)
        {
            destination.Write(buffer, 0, bytesRead);
        }
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await ReadAsync(buffer, 0, bufferSize, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
        }
    }

    public async Task CopyFromAsync(Stream source, int bufferSize = 4 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        int numDisks = FileStreams.Count;

        int realDataDisks = numDisks - 1;

        int[] bytesRead = new int[realDataDisks];
        int stripeCount = 0;
        long oldPosition = _position;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            using MemoryStream bufferStream = await source.ReadStreamWithLimitAsync(realDataDisks * 1024 * 1024);
            hasMoreData = bufferStream.Length > 0;

            while ((bytesRead[0] = await bufferStream.ReadAsync(_readPooledArrays[0], 0, _stripeSize, cancellationToken)) > 0)
            {
                for (int i = 1; i < realDataDisks; i++)
                {
                    bytesRead[i] = await bufferStream.ReadAsync(_readPooledArrays[i], 0, _stripeSize, cancellationToken);
                }

                for (int i = 0; i < _parityPoolBuffers.Length; i++)
                {
                    _readPooledArrays[i].CopyTo(_parityPoolBuffers[i], 0);
                }

                _parityPoolBuffers.XorParity(_readPooledArrays[realDataDisks]);
                numDisks.GenerateRaid5Indices(stripeCount, _indicesArrayPool);
                await FileStreams.WriteTasks(_indicesArrayPool, _stripeSize, cancellationToken, _readPooledArrays);

                var totalRead = bytesRead.Sum();
                _position += totalRead;
                stripeCount++;
            }
        }

        _originalSize = _position - oldPosition;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalFileStream = FileStreams.Count;

        int totalBytesWritten = 0;
        var writeBufferSize = (int)Math.Ceiling((float)count / ActualBufferSize) * ActualBufferSize;
        byte[] writeBuffer = new byte[writeBufferSize];

        count = (int)Math.Min(count, _originalSize);
        int[] byteReads = new int[totalFileStream];

        while (totalBytesWritten < writeBufferSize)
        {
            byteReads.Fill(0);
            totalFileStream.GenerateRaid5Indices(StripeRowIndex, _indicesArrayPool);
            ReadAndRecoverData(_indicesArrayPool, _readPooledArrays, byteReads, _parityPoolBuffers);


            for (int i = 0; i < totalFileStream - 1; i++)
            {
                if (_position >= _originalSize || totalBytesWritten > count) break;

                var readSize = byteReads[i];
                var writeSize1 = (int)Math.Min(_originalSize - _position, readSize);
                Array.Copy(_readPooledArrays[i], 0, writeBuffer, totalBytesWritten, writeSize1);
                _position += readSize;
                totalBytesWritten += readSize;
                StripeBlockIndex++;
            }

            StripeRowIndex++;
        }

        int copySize = Math.Min(count, totalBytesWritten);
        Array.Copy(writeBuffer, offset + StartPaddingSize, buffer, 0, copySize);
        StartPaddingSize = 0;
        return copySize;
    }

    #region read data with recovery

    private async Task ReadAndRecoverDataAsync(int[] indices, byte[][] readBuffers, int[] readBytes, byte[][] parityBuffers, CancellationToken cancellationToken)
    {
        int errorIndex = -1;
        var indicesLength = readBytes.Length;
        var lastIndex = indicesLength - 1;

        for (int i = 0; i < indicesLength; i++)
        {
            var stripIndex = indices[i];
            var stream = FileStreams[i];
            if (stream != null)
            {
                readBytes[stripIndex] = await stream.ReadAsync(readBuffers[stripIndex], 0, _stripeSize, cancellationToken);
            }
            else
            {
                if (i != lastIndex)
                {
                    errorIndex = i;
                }

                readBytes[stripIndex] = 0;
            }
        }


        if (errorIndex != -1)
        {
            int parityIndex = 0;
            for (int i = 0; i < indicesLength; i++)
            {
                if (i == errorIndex)
                    continue;
                readBuffers[i].CopyTo(parityBuffers[parityIndex++], 0);
            }

            readBytes[errorIndex] = parityBuffers.Max(x => x.Length);
            parityBuffers.XorParity(readBuffers[errorIndex]);
        }
    }

    private void ReadAndRecoverData(int[] indices, byte[][] readBuffers, int[] readBytes, byte[][] parityBuffers)
    {
        int errorIndex = -1;
        var indicesLength = indices.Length;
        var lastIndex = indicesLength - 1;

        for (int i = 0; i < indicesLength; i++)
        {
            var stripIndex = indices[i];
            var stream = FileStreams[i];
            if (stream != null)
            {
                readBytes[stripIndex] = stream.Read(readBuffers[stripIndex], 0, _stripeSize);
            }
            else
            {
                if (i != lastIndex)
                {
                    errorIndex = i;
                }

                readBytes[stripIndex] = 0;
            }
        }

        if (errorIndex != -1)
        {
            int parityIndex = 0;
            for (int i = 0; i < indicesLength; i++)
            {
                if (i == errorIndex)
                    continue;
                readBuffers[i].CopyTo(parityBuffers[parityIndex++], 0);
            }

            readBytes[errorIndex] = parityBuffers.Max(x => x.Length);
            parityBuffers.XorParity(readBuffers[errorIndex]);
        }
    }

    #endregion

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalFileStream = FileStreams.Count;

        int totalBytesWritten = 0;
        var writeBufferSize = (int)Math.Ceiling((float)count / ActualBufferSize) * ActualBufferSize;
        byte[] writeBuffer = new byte[writeBufferSize];

        count = (int)Math.Min(count, _originalSize);
        int[] byteReads = new int[totalFileStream];

        while (count > totalBytesWritten && _position < _originalSize && writeBuffer.Length > totalBytesWritten)
        {
            byteReads.Fill(0);
            totalFileStream.GenerateRaid5Indices(StripeRowIndex, _indicesArrayPool);
            await ReadAndRecoverDataAsync(_indicesArrayPool, _readPooledArrays, byteReads, _parityPoolBuffers, cancellationToken);

            for (int i = 0; i < totalFileStream - 1; i++)
            {
                var readSize = byteReads[i];
                var writeSize1 = (int)Math.Min(_originalSize - _position, readSize);
                if (writeBuffer.Length <= totalBytesWritten) break;
                Array.Copy(_readPooledArrays[i], 0, writeBuffer, totalBytesWritten, writeSize1);
                _position += writeSize1;
                totalBytesWritten += writeSize1;
                StripeBlockIndex++;
            }

            StripeRowIndex++;
        }

        int copySize = Math.Min(count, totalBytesWritten);
        Array.Copy(writeBuffer, offset + StartPaddingSize, buffer, 0, copySize);
        StartPaddingSize = 0;
        return copySize;
    }

    public override void Flush()
    {
        foreach (var stream in FileStreams)
        {
            if (stream != null)
                stream.Flush();
        }
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        List<Task> tasks = new List<Task>();
        foreach (var stream in FileStreams)
        {
            if (stream != null) tasks.Add(stream.FlushAsync(cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    public override void SetLength(long value)
    {
        //
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            for (int i = 0; i < FileStreams.Count; i++)
            {
                FileStreams[i]?.Dispose();
            }

            // Return rented arrays to the pool
            foreach (var array in _readPooledArrays)
            {
                ByteArrayPool.Return(array, clearArray: true); // Clear array if needed for security
            }

            foreach (var array in _parityPoolBuffers)
            {
                ByteArrayPool.Return(array, clearArray: true); // Clear array if needed for security
            }

            IntArrayPool.Return(_indicesArrayPool, clearArray: true); // Clear array if needed for security
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        List<Task> tasks = new List<Task>();
        foreach (var stream in FileStreams)
        {
            if (stream != null)
                tasks.Add(stream.DisposeAsync().AsTask());
        }

        foreach (var array in _readPooledArrays)
        {
            ByteArrayPool.Return(array, clearArray: true); // Clear array if needed for security
        }

        foreach (var array in _parityPoolBuffers)
        {
            ByteArrayPool.Return(array, clearArray: true); // Clear array if needed for security
        }

        IntArrayPool.Return(_indicesArrayPool, clearArray: true); // Clear array if needed for security


        await Task.WhenAll(tasks);
    }
}
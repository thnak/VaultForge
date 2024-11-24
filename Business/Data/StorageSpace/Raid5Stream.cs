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
        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
        ActualBufferSize = _stripeSize * (FileStreams.Count - 1);
        _readPooledArrays = new byte[FileStreams.Count][];
        _parityPoolBuffers = new byte[FileStreams.Count - 1][];
        _indicesArrayPool = IntArrayPool.Rent(1);
        int realDataDisks = FileStreams.Count - 1;
        for (int i = 0; i < FileStreams.Count; i++)
        {
            _readPooledArrays[i] = ByteArrayPool.Rent(stripeSize);
        }

        for (int i = 0; i < realDataDisks; i++)
        {
            _parityPoolBuffers[i] = ByteArrayPool.Rent(stripeSize);
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
        // Calculate the new position based on the SeekOrigin
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = offset;
                break;
            case SeekOrigin.Current:
                _position += offset;
                break;
            case SeekOrigin.End:
                _position = _originalSize + offset;
                break;
            default:
                throw new ArgumentException(@"Invalid SeekOrigin", nameof(origin));
        }

        // Validate that the new position is within the bounds of the file
        if (_position < 0 || _position > _originalSize)
            throw new ArgumentOutOfRangeException(nameof(offset), @"Seek position is out of range.");

        int availableNumDisks = FileStreams.Count - 1;

        StripeRowIndex = (int)(_position / availableNumDisks / _stripeSize);
        StripeBlockIndex = _position / _stripeSize;
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

        // Seek each file stream to the start of the stripe because we always read from stripe index
        var seekPosition = StripeRowIndex * _stripeSize;
        FileStreams.Seek(seekPosition, SeekOrigin.Begin);

        // update position of raid5Stream
        var startOriginStripRow = StripeRowIndex * availableNumDisks * _stripeSize;
        // this one will use to skip when read for the first time after seek
        StartPaddingSize = (int)_position - startOriginStripRow;
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
        long oldPosition = _position;
        bool hasMoreData = true;
        // buffer use to read stream pipe send from client to prevent data loss issue
        await using var writeBuffer = new MemoryStream(realDataDisks * 1024 * 1024);
        while (hasMoreData)
        {
            await source.ReadStreamWithLimitAsync(writeBuffer, _readPooledArrays[0]);
            hasMoreData = writeBuffer.Length > 0;

            while ((bytesRead[0] = await writeBuffer.ReadAsync(_readPooledArrays[0], 0, _stripeSize, cancellationToken)) > 0)
            {
                for (int i = 1; i < realDataDisks; i++)
                {
                    bytesRead[i] = await writeBuffer.ReadAsync(_readPooledArrays[i], 0, _stripeSize, cancellationToken);
                }

                for (int i = 0; i < _parityPoolBuffers.Length; i++)
                {
                    _readPooledArrays[i].CopyTo(_parityPoolBuffers[i], 0);
                }

                _parityPoolBuffers.XorParity(_readPooledArrays[realDataDisks]);
                numDisks.GenerateRaid5Indices(StripeRowIndex, _indicesArrayPool);
                await FileStreams.WriteTasks(_indicesArrayPool, _stripeSize, cancellationToken, _readPooledArrays);

                var totalRead = bytesRead.Sum();
                _position += totalRead;
                StripeRowIndex++;
            }
        }

        _originalSize = _position - oldPosition;
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
        var indicesLength = readBytes.Length;
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalFileStream = FileStreams.Count;
        int totalFileDataStream = totalFileStream - 1;

        int totalByteReads = 0;
        int totalByteWrite2Buffer = 0;
        int totalBytesReadEachStripeRow = 0;
        StartPaddingSize = offset + StartPaddingSize;
        byte[] writeBuffer = new byte[ActualBufferSize];

        count = (int)Math.Max(Math.Min(count - StartPaddingSize, _originalSize - _position), 0);
        int[] byteReads = new int[totalFileStream];


        while (totalByteWrite2Buffer < count)
        {
            byteReads.Fill(0);
            totalFileStream.GenerateRaid5Indices(StripeRowIndex, _indicesArrayPool);
            ReadAndRecoverData(_indicesArrayPool, _readPooledArrays, byteReads, _parityPoolBuffers);

            totalBytesReadEachStripeRow = 0;
            for (int i = 0; i < totalFileDataStream; i++)
            {
                var readSize = byteReads[i];
                Array.Copy(_readPooledArrays[i], 0, writeBuffer, totalBytesReadEachStripeRow, readSize);
                totalBytesReadEachStripeRow += readSize;
                StripeBlockIndex++;
            }

            StripeRowIndex++;
            totalBytesReadEachStripeRow -= StartPaddingSize;
            totalBytesReadEachStripeRow = Math.Min((int)(_originalSize - _position), totalBytesReadEachStripeRow);
            totalBytesReadEachStripeRow = Math.Min(totalBytesReadEachStripeRow, buffer.Length - totalByteWrite2Buffer);
            Array.Copy(writeBuffer, StartPaddingSize, buffer, totalByteWrite2Buffer, totalBytesReadEachStripeRow);
            totalByteWrite2Buffer += totalBytesReadEachStripeRow;
            totalByteReads += totalBytesReadEachStripeRow;
            StartPaddingSize = 0;
            _position += totalBytesReadEachStripeRow;
            if (totalBytesReadEachStripeRow == 0) break;
        }

        return totalByteReads;
    }


    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalFileStream = FileStreams.Count;
        int totalFileDataStream = totalFileStream - 1;

        int totalByteReads = 0;
        int totalByteWrite2Buffer = 0;
        int totalBytesReadEachStripeRow = 0;
        StartPaddingSize = offset + StartPaddingSize;
        byte[] writeBuffer = new byte[ActualBufferSize];

        count = (int)Math.Max(Math.Min(count - StartPaddingSize, _originalSize - _position), 0);
        int[] byteReads = new int[totalFileStream];


        while (totalByteWrite2Buffer < count)
        {
            byteReads.Fill(0);
            totalFileStream.GenerateRaid5Indices(StripeRowIndex, _indicesArrayPool);
            await ReadAndRecoverDataAsync(_indicesArrayPool, _readPooledArrays, byteReads, _parityPoolBuffers, cancellationToken);

            totalBytesReadEachStripeRow = 0;
            for (int i = 0; i < totalFileDataStream; i++)
            {
                var readSize = byteReads[i];
                Array.Copy(_readPooledArrays[i], 0, writeBuffer, totalBytesReadEachStripeRow, readSize);
                totalBytesReadEachStripeRow += readSize;
                StripeBlockIndex++;
            }

            StripeRowIndex++;
            totalBytesReadEachStripeRow -= StartPaddingSize;
            totalBytesReadEachStripeRow = Math.Min((int)(_originalSize - _position), totalBytesReadEachStripeRow);
            totalBytesReadEachStripeRow = Math.Min(totalBytesReadEachStripeRow, buffer.Length - totalByteWrite2Buffer);
            Array.Copy(writeBuffer, StartPaddingSize, buffer, totalByteWrite2Buffer, totalBytesReadEachStripeRow);
            totalByteWrite2Buffer += totalBytesReadEachStripeRow;
            totalByteReads += totalBytesReadEachStripeRow;
            StartPaddingSize = 0;
            _position += totalBytesReadEachStripeRow;
            if (totalBytesReadEachStripeRow == 0) break;
        }

        return totalByteReads;
    }

    public override void Flush()
    {
        foreach (var stream in FileStreams)
        {
            stream?.Flush();
        }
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        foreach (var stream in FileStreams)
        {
            if (stream != null) await stream.FlushAsync(cancellationToken);
        }
    }

    public override void SetLength(long value)
    {
        if (value == 0)
        {
            foreach (var stream in FileStreams)
            {
                if (stream != null) stream.SetLength(value);
            }
            _originalSize = value;
            _position = value;
        }
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
        foreach (var stream in FileStreams)
        {
            if (stream != null)
                await stream.DisposeAsync();
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
    }
}
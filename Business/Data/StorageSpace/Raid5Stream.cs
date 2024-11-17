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

    public List<FileStream?> FileStreams { get; set; }

    public Raid5Stream(IEnumerable<string> path, long originalSize, int stripeSize, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
    {
        FileStreams = path.OpenFile(fileMode, fileAccess, fileShare, bufferSize: 4 * stripeSize);
        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
        ActualBufferSize = _stripeSize * (FileStreams.Count - 1);
    }

    private long FindStripeIndex(long size)
    {
        if (size > _stripeSize)
        {
            var index = size / _stripeSize;
            return index;
        }

        return 0;
    }

    private long StripeIndexBufferSize(long size)
    {
        if (size > _stripeSize)
            size %= _stripeSize;
        return _stripeSize - size;
    }

    public override bool CanSeek => true;
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override long Length => _originalSize;

    public override long Position
    {
        get => _position;
        set { _position = value; }
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
            bytesRead = Math.Min(bufferSize, bytesRead);
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
        }
    }

    public async Task CopyFromAsync(Stream source, int bufferSize = 4 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        int numDisks = FileStreams.Count;
        byte[][] buffers = Enumerable.Range(0, numDisks).Select(_ => new byte[_stripeSize]).ToArray();
        byte[][] subset = Enumerable.Range(0, numDisks - 1).Select(_ => new byte[_stripeSize]).ToArray();

        int realDataDisks = numDisks - 1;

        int[] bytesRead = new int[numDisks];
        int stripeCount = 0;

        bool hasMoreData = true;

        while (hasMoreData)
        {
            using MemoryStream bufferStream = await source.ReadStreamWithLimitAsync(bufferSize);
            hasMoreData = bufferStream.Length > 0;

            while ((bytesRead[0] = await bufferStream.ReadAsync(buffers[0], 0, _stripeSize, cancellationToken)) > 0)
            {
                for (int i = 1; i < realDataDisks; i++)
                {
                    bytesRead[i] = await bufferStream.ReadAsync(buffers[i], 0, _stripeSize, cancellationToken);
                }

                for (int i = 0; i < subset.Length; i++)
                {
                    buffers[i].CopyTo(subset[i], 0);
                }

                var realBytesRead = bytesRead.Take(realDataDisks).ToArray();
                bytesRead[realDataDisks] = realBytesRead.Max();
                buffers[realDataDisks] = subset.XorParity();
                var indices = numDisks.GenerateRaid5Indices(stripeCount);
                var writeTasks = FileStreams.CreateWriteTasks(indices, _stripeSize, cancellationToken, buffers);
                await Task.WhenAll(writeTasks);

                var totalRead = realBytesRead.Sum();
                _position += totalRead;
                _originalSize += totalRead;
                stripeCount++;
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalFileStream = FileStreams.Count;
        byte[][] readBuffers = Enumerable.Range(0, totalFileStream).Select(_ => new byte[_stripeSize]).ToArray();
        byte[][] parityBuffers = Enumerable.Range(0, totalFileStream - 1).Select(_ => new byte[_stripeSize]).ToArray();

        int totalBytesWritten = 0;
        var writeBufferSize = (int)Math.Ceiling((float)count / ActualBufferSize) * ActualBufferSize;
        byte[] writeBuffer = new byte[writeBufferSize];

        count = (int)Math.Min(count, _originalSize);
        int[] byteReads = new int[totalFileStream];

        while (totalBytesWritten < writeBufferSize)
        {
            byteReads.Fill(0);
            var indices = totalFileStream.GenerateRaid5Indices(StripeRowIndex);
            ReadAndRecoverData(indices, readBuffers, byteReads, parityBuffers);

            if (byteReads.Sum() == 0)
            {
                break; // End of stream
            }

            for (int i = 0; i < totalFileStream - 1; i++)
            {
                if (_position >= _originalSize || totalBytesWritten > count) break;

                var readSize = byteReads[i];
                var writeSize1 = (int)Math.Min(_originalSize - _position, readSize);
                Array.Copy(readBuffers[i], 0, writeBuffer, totalBytesWritten, writeSize1);
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
        var indicesLength = indices.Length;
        var lastIndex = indicesLength - 1;
        Task<int>[] tasks = new Task<int>[indicesLength];

        for (int i = 0; i < indicesLength; i++)
        {
            var stripIndex = indices[i];
            var stream = FileStreams[i];
            if (stream != null)
            {
                tasks[stripIndex] = stream.ReadAsync(readBuffers[stripIndex], 0, _stripeSize, cancellationToken);
            }
            else
            {
                if (i != lastIndex)
                {
                    errorIndex = i;
                }

                tasks[stripIndex] = Task.FromResult(0);
            }
        }

        await Task.WhenAll(tasks);

        for (int i = 0; i < indicesLength; i++)
        {
            readBytes[i] = await tasks[i];
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
            readBuffers[errorIndex] = parityBuffers.XorParity();
        }
    }

    private void ReadAndRecoverData(int[] indices, byte[][] readBuffers, int[] readBytes, byte[][] parityBuffers)
    {
        int errorIndex = -1;
        var indicesLength = indices.Length;
        var lastIndex = indicesLength - 1;
        // int[] tasks = new int[indicesLength];

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
            readBuffers[errorIndex] = parityBuffers.XorParity();
        }
    }

    #endregion

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalFileStream = FileStreams.Count;
        byte[][] readBuffers = Enumerable.Range(0, totalFileStream).Select(_ => new byte[_stripeSize]).ToArray();
        byte[][] parityBuffers = Enumerable.Range(0, totalFileStream - 1).Select(_ => new byte[_stripeSize]).ToArray();

        int totalBytesWritten = 0;
        var writeBufferSize = (int)Math.Ceiling((float)count / ActualBufferSize) * ActualBufferSize;
        byte[] writeBuffer = new byte[writeBufferSize];

        count = (int)Math.Min(count, _originalSize);
        int[] byteReads = new int[totalFileStream];

        while (totalBytesWritten < writeBufferSize)
        {
            byteReads.Fill(0);
            var indices = totalFileStream.GenerateRaid5Indices(StripeRowIndex);
            await ReadAndRecoverDataAsync(indices, readBuffers, byteReads, parityBuffers, cancellationToken);

            if (byteReads.Sum() == 0)
            {
                break; // End of stream
            }

            for (int i = 0; i < totalFileStream - 1; i++)
            {
                if (_position >= _originalSize || totalBytesWritten > count) break;

                var readSize = byteReads[i];
                var writeSize1 = (int)Math.Min(_originalSize - _position, readSize);
                Array.Copy(readBuffers[i], 0, writeBuffer, totalBytesWritten, writeSize1);
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
            FileStreams[0]?.Dispose();
            FileStreams[1]?.Dispose();
            FileStreams[2]?.Dispose();
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

        await Task.WhenAll(tasks);
    }
}
using Business.Data.StorageSpace.Utils;
using Business.Utils;
using BusinessModels.Resources;

namespace Business.Data.StorageSpace;

public class Raid5Stream : Stream
{
    private long _originalSize;
    private readonly int _stripeSize;
    private long _position;
    private long StripeIndex { get; set; }
    private int StripeRowIndex { get; set; }
    private long StartPadStripIndex { get; set; }
    private int StartPadding { get; set; }
    private int StartPaddingSize { get; set; }


    public List<FileStream?> FileStreams { get; set; }
    private readonly int _readWriteBufferSize = 10 * 1024;

    public Raid5Stream(IEnumerable<string> path, long originalSize, int stripeSize, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
    {
        FileStreams = path.OpenFile(fileMode, fileAccess, fileShare, bufferSize: _readWriteBufferSize);
        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
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

    private int StripeIndexStart(long size)
    {
        return _stripeSize - (int)StripeIndexBufferSize(size);
    }

    public override bool CanSeek => true;
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override long Length => _originalSize;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _originalSize)
                throw new ArgumentOutOfRangeException(nameof(value), AppLang.Raid5Stream_Position_Position_is_out_of_range_);
            _position = value;
        }
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

        // Calculate which stripe the new position falls into and the offset within that stripe
        StripeRowIndex = (int)(newPosition / 2 / _stripeSize);

        StripeIndex = newPosition / _stripeSize;
        if (StripeIndex > 1)
        {
            if (StripeIndex % 2 != 0)
            {
                StripeIndex -= 1;
            }
        }
        else
        {
            StripeIndex = 0;
        }

        // Seek each file stream to the start of the stripe
        var seekPosition = StripeRowIndex * _stripeSize;

        FileStreams.Seek(seekPosition, SeekOrigin.Begin);

        // Adjust the read pointers to account for the position within the stripe
        _position = newPosition;
        StartPadStripIndex = (int)FindStripeIndex(newPosition);

        StartPaddingSize = (int)StripeIndexBufferSize(newPosition);
        StartPadding = StripeIndexStart(newPosition);

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
        byte[][] buffers = Enumerable.Range(0, numDisks).Select(_ => new byte[_stripeSize]).ToArray();

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

                byte[][] subset = buffers.Take(realDataDisks).ToArray();
                var realBytesRead = bytesRead.Take(realDataDisks).ToArray();
                bytesRead[realDataDisks] = realBytesRead.Max();
                buffers[realDataDisks] = subset.XorParity();

                var writeTasks = FileStreams.CreateWriteTasks(stripeCount, bytesRead, cancellationToken, buffers);
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

        int totalBytesWritten = 0;

        count = (int)Math.Min(count, _originalSize);

        StartPaddingSize = Math.Min(StartPaddingSize, count);

        while (totalBytesWritten < count)
        {
            int[] readTasks = new int[totalFileStream];

            // Determine the current stripe pattern and read from available files
            switch (StripeRowIndex % totalFileStream)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    if (FileStreams[0] == null)
                    {
                        // Recover data1 using parity and data2
                        readTasks[1] = FileStreams[1]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[0] = FileStreams[2]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (FileStreams[1] == null)
                    {
                        // Recover data2 using parity and data1
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (FileStreams[2] == null)
                    {
                        // Read data, calculate parity to verify correctness
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[1]!.Read(readBuffers[1], 0, _stripeSize);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[1]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[2] = FileStreams[2]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                    }

                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    if (FileStreams[0] == null)
                    {
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[0] = FileStreams[1]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (FileStreams[2] == null)
                    {
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[1]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (FileStreams[1] == null)
                    {
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[2] = FileStreams[1]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                    }

                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    if (FileStreams[1] == null)
                    {
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[0] = FileStreams[0]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (FileStreams[2] == null)
                    {
                        readTasks[0] = FileStreams[1]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[0]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (FileStreams[0] == null)
                    {
                        readTasks[0] = FileStreams[1]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[1]!.Read(readBuffers[0], 0, _stripeSize);
                        readTasks[1] = FileStreams[2]!.Read(readBuffers[1], 0, _stripeSize);
                        readTasks[2] = FileStreams[0]!.Read(readBuffers[totalFileStream - 1], 0, _stripeSize);
                    }

                    break;
            }

            var bytesRead1 = readTasks[0];
            var bytesRead2 = readTasks[1];

            if (bytesRead1 == 0 && bytesRead2 == 0)
            {
                break; // End of stream
            }


            var writeSize1 = (int)Math.Min(_originalSize - _position, bytesRead1);

            if (StripeIndex == StartPadStripIndex)
            {
                writeSize1 = Math.Min(StartPaddingSize, count - totalBytesWritten);
                Array.Copy(readBuffers[0], StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                totalBytesWritten += writeSize1;
            }
            else if (StripeIndex >= StartPadStripIndex)
            {
                writeSize1 = Math.Min(writeSize1, count - totalBytesWritten);
                Array.Copy(readBuffers[0], 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                totalBytesWritten += writeSize1;
            }

            _position += writeSize1;
            StripeIndex++;

            var writeSize2 = (int)Math.Min(_originalSize - _position, bytesRead2);

            if (StripeIndex == StartPadStripIndex)
            {
                writeSize2 = Math.Min(StartPaddingSize, count - totalBytesWritten);
                Array.Copy(readBuffers[1], StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
                totalBytesWritten += writeSize2;
            }

            else if (StripeIndex >= StartPadStripIndex)
            {
                writeSize2 = Math.Min(writeSize2, count - totalBytesWritten);
                Array.Copy(readBuffers[1], 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
                totalBytesWritten += writeSize2;
            }

            _position += writeSize2;
            StripeIndex++;
            StripeRowIndex++;
        }

        return totalBytesWritten;
    }

    private async Task ReadAndRecoverDataAsync(int[] indices, byte[][] readBuffers, int[] readBytes, CancellationToken cancellationToken)
    {
        var readTasks = new Task<int>[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            readTasks[i] = FileStreams[indices[i]]?.ReadAsync(readBuffers[i], 0, _stripeSize, cancellationToken) ?? Task.FromResult(0);
        }

        await Task.WhenAll(readTasks);

        // update read bytes
        for (int i = 0; i < indices.Length; i++)
        {
            readBytes[i] = await readTasks[i];
        }

        // If there's a missing file, recover it using parity
        var lastIndicesIndex = indices.Length - 1;
        if (FileStreams[indices[lastIndicesIndex]] == null)
        {
            readBuffers[indices[lastIndicesIndex]] = readBuffers.Take(lastIndicesIndex).ToArray().XorParity();
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalFileStream = FileStreams.Count;
        byte[][] readBuffers = Enumerable.Range(0, totalFileStream).Select(_ => new byte[_stripeSize]).ToArray();

        int totalBytesWritten = 0;

        count = (int)Math.Min(count, _originalSize);

        StartPaddingSize = Math.Min(StartPaddingSize, count);

        while (totalBytesWritten < count)
        {
            int[] byteReads = new int[totalFileStream];
            var indices = totalFileStream.GenerateRaid5Indices(StripeRowIndex);
            await ReadAndRecoverDataAsync(indices, readBuffers, byteReads, cancellationToken);

            if (byteReads.Sum() == 0)
            {
                break; // End of stream
            }

            var byteReadArray = byteReads.Take(totalFileStream - 1).ToArray();
            for (int i = 0; i < byteReadArray.Length; i++)
            {
                var readSize = byteReadArray[i];
                var writeSize1 = (int)Math.Min(_originalSize - _position, readSize);

                if (StripeIndex == StartPadStripIndex)
                {
                    writeSize1 = Math.Min(StartPaddingSize, count - totalBytesWritten);
                    Array.Copy(readBuffers[i], StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                    totalBytesWritten += writeSize1;
                }
                else if (StripeIndex >= StartPadStripIndex)
                {
                    writeSize1 = Math.Min(writeSize1, count - totalBytesWritten);
                    Array.Copy(readBuffers[i], 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                    totalBytesWritten += writeSize1;
                }

                _position += writeSize1;
                StripeIndex++;
            }

            StripeRowIndex++;
        }

        _originalSize += totalBytesWritten;

        return totalBytesWritten;
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
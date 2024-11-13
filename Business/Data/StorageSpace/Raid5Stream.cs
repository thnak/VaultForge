using Business.Data.StorageSpace.Utils;
using Business.Utils;
using BusinessModels.Resources;

namespace Business.Data.StorageSpace;

public class Raid5Stream : Stream
{
    private readonly long _originalSize;
    private readonly int _stripeSize;
    private long _position;
    private long StripeIndex { get; set; }
    private long StripeRowIndex { get; set; }
    private long StartPadStripIndex { get; set; }
    private long EndPadStripIndex { get; set; }
    private int StartPadding { get; set; }
    private int EndPaddingSize { get; set; }
    private int StartPaddingSize { get; set; }

    private bool isFile1Corrupted;
    private bool isFile2Corrupted;
    private bool isFile3Corrupted;

    // private FileStream? file1;
    // private FileStream? file2;
    // private FileStream? file3;
    private FileStream?[] FileStreams { get; set; } = [];
    private int _readWriteBufferSize = 10 * 1024;


    public Raid5Stream(string file1Path, string file2Path, string file3Path, long originalSize, int stripeSize)
    {
        isFile1Corrupted = !File.Exists(file1Path) || string.IsNullOrEmpty(file1Path);
        isFile2Corrupted = !File.Exists(file2Path) || string.IsNullOrEmpty(file2Path);
        isFile3Corrupted = !File.Exists(file3Path) || string.IsNullOrEmpty(file3Path);

        List<string> path = [file1Path, file2Path, file3Path];
        FileStreams = path.OpenFile(FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize);

        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
    }

    public Raid5Stream(List<string> path, long originalSize, int stripeSize)
    {
        FileStreams = path.OpenFile(FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize);
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
                throw new ArgumentException("Invalid SeekOrigin", nameof(origin));
        }

        // Validate that the new position is within the bounds of the file
        if (newPosition < 0 || newPosition > _originalSize)
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek position is out of range.");

        // Calculate which stripe the new position falls into and the offset within that stripe
        StripeRowIndex = newPosition / 2 / _stripeSize;

        StripeIndex = newPosition / _stripeSize;
        if (StripeIndex > 1)
        {
            if (StripeIndex % 2 != 0)
            {
                StripeIndex -= 1;
            }
            // else
            // {
            //     StripeIndex -= 2;
            // }
        }
        else
        {
            StripeIndex = 0;
        }
        // StripeIndex = (int)FindStripeIndex(newPosition);

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


    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count).Result; // Call the async version synchronously
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalFileStream = FileStreams.Length;
        byte[][] readBuffers = Enumerable.Range(0, totalFileStream).Select(_ => new byte[_stripeSize]).ToArray();

        int totalBytesWritten = 0;

        count = (int)Math.Min(count, _originalSize);

        var newPosition = Math.Min(_position + count, _originalSize);
        EndPadStripIndex = FindStripeIndex(newPosition);
        EndPaddingSize = (int)(newPosition % _stripeSize);
        StartPaddingSize = Math.Min(StartPaddingSize, count);

        while (totalBytesWritten < count)
        {
            Task<int>[] readTasks = new Task<int>[totalFileStream];

            // Determine the current stripe pattern and read from available files
            switch (StripeRowIndex % totalFileStream)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    if (isFile1Corrupted)
                    {
                        // Recover data1 using parity and data2
                        readTasks[1] = FileStreams[1]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[0] = FileStreams[2]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (isFile2Corrupted)
                    {
                        // Recover data2 using parity and data1
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (isFile3Corrupted)
                    {
                        // Read data, calculate parity to verify correctness
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[1]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[1]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[2] = FileStreams[2]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1], readTasks[2]);
                    }

                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    if (isFile1Corrupted)
                    {
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[0] = FileStreams[1]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[1]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (isFile2Corrupted)
                    {
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[2] = FileStreams[1]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1], readTasks[2]);
                    }

                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    if (isFile2Corrupted)
                    {
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[0] = FileStreams[0]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                        readBuffers[0] = readBuffers[totalFileStream - 1].XorParity(readBuffers[1]);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTasks[0] = FileStreams[1]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[0]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                        readBuffers[1] = readBuffers[totalFileStream - 1].XorParity(readBuffers[0]);
                    }
                    else if (isFile1Corrupted)
                    {
                        readTasks[0] = FileStreams[1]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1]);
                    }
                    else
                    {
                        readTasks[0] = FileStreams[1]!.ReadAsync(readBuffers[0], 0, _stripeSize, cancellationToken);
                        readTasks[1] = FileStreams[2]!.ReadAsync(readBuffers[1], 0, _stripeSize, cancellationToken);
                        readTasks[2] = FileStreams[0]!.ReadAsync(readBuffers[totalFileStream - 1], 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTasks[0], readTasks[1], readTasks[2]);
                    }

                    break;
            }

            var bytesRead1 = await readTasks[0];
            var bytesRead2 = await readTasks[1];

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


    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
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
        if (FileStreams[0] != null) await FileStreams[0]!.DisposeAsync();
        if (FileStreams[1] != null) await FileStreams[1]!.DisposeAsync();
        if (FileStreams[2] != null) await FileStreams[2]!.DisposeAsync();
        await base.DisposeAsync();
    }
}
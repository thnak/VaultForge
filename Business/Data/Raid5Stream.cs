using Business.Utils;

namespace Business.Data;

public class Raid5Stream : Stream
{
    private readonly long _originalSize;
    private readonly int _stripeSize;
    private long _position;
    private long StripeIndex { get; set; }
    private long StartPadStripIndex { get; set; }
    private long EndPadStripIndex { get; set; }
    private int StartPadding { get; set; }
    private int EndPaddingSize { get; set; }
    private int StartPaddingSize { get; set; }

    private bool isFile1Corrupted;
    private bool isFile2Corrupted;
    private bool isFile3Corrupted;
    private FileStream? file1;
    private FileStream? file2;
    private FileStream? file3;
    private int _readWriteBufferSize = 10 * 1024;


    public Raid5Stream(string file1Path, string file2Path, string file3Path, long originalSize, int stripeSize)
    {
        isFile1Corrupted = !File.Exists(file1Path) || string.IsNullOrEmpty(file1Path);
        isFile2Corrupted = !File.Exists(file2Path) || string.IsNullOrEmpty(file2Path);
        isFile3Corrupted = !File.Exists(file3Path) || string.IsNullOrEmpty(file3Path);


        file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);
        file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);
        file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);


        _originalSize = originalSize;
        _stripeSize = stripeSize;
        _position = 0;
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
                throw new ArgumentOutOfRangeException(nameof(value), "Position is out of range.");
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
        StripeIndex = newPosition / 2 / _stripeSize;
        StartPadStripIndex = newPosition / _stripeSize;

        // Seek each file stream to the start of the stripe
        var seekPosition = StripeIndex / _stripeSize;
        file1?.Seek(seekPosition, SeekOrigin.Begin);
        file2?.Seek(seekPosition, SeekOrigin.Begin);
        file3?.Seek(seekPosition, SeekOrigin.Begin);

        // Adjust the read pointers to account for the position within the stripe
        _position = newPosition;
        StartPadding = ((int)newPosition % _stripeSize) - 0;
        StartPaddingSize = _stripeSize - StartPadding;

        return _position;
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count).Result; // Call the async version synchronously
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        byte[] buffer1 = new byte[_stripeSize];
        byte[] buffer2 = new byte[_stripeSize];
        byte[] parityBuffer = new byte[_stripeSize];

        int totalBytesWritten = 0;

        var newPosition = _position + count;
        EndPadStripIndex = newPosition / _stripeSize;
        EndPaddingSize = (int)(newPosition % _stripeSize);


        while (totalBytesWritten < count)
        {
            Task<int> readTask1 = Task.FromResult(0);
            Task<int> readTask2 = Task.FromResult(0);
            Task<int> readTask3;


            // Determine the current stripe pattern and read from available files
            switch (StripeIndex % 3)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    if (isFile1Corrupted)
                    {
                        // Recover data1 using parity and data2
                        readTask2 = file2!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask1 = file3!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile2Corrupted)
                    {
                        // Recover data2 using parity and data1
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file3!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile3Corrupted)
                    {
                        // Read data, calculate parity to verify correctness
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file2!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file2!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask3 = file3!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    if (isFile1Corrupted)
                    {
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask1 = file2!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file2!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile2Corrupted)
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask3 = file2!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    if (isFile2Corrupted)
                    {
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask1 = file1!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file1!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile1Corrupted)
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, _stripeSize, cancellationToken);
                        readTask2 = file3!.ReadAsync(buffer2, 0, _stripeSize, cancellationToken);
                        readTask3 = file1!.ReadAsync(parityBuffer, 0, _stripeSize, cancellationToken);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;
            }

            var bytesRead1 = await readTask1;
            var bytesRead2 = await readTask2;

            if (bytesRead1 == 0 && bytesRead2 == 0)
            {
                break; // End of stream
            }

            if (totalBytesWritten >= count) break;

            var writeSize1 = (int)Math.Min(_originalSize - _position, bytesRead1);

            if (StripeIndex == StartPadStripIndex)
            {
                writeSize1 = StartPaddingSize;
                if (StripeIndex == EndPadStripIndex)
                {
                    writeSize1 = Math.Min(EndPaddingSize, writeSize1);
                    Array.Copy(buffer1, StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                    totalBytesWritten += writeSize1;
                    _position += writeSize1;
                    StripeIndex++;
                    break;
                }

                Array.Copy(buffer1, StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
            }
            else if (StripeIndex == EndPadStripIndex)
            {
                writeSize1 = EndPaddingSize;
                Array.Copy(buffer1, 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                totalBytesWritten += writeSize1;
                _position += writeSize1;
                StripeIndex++;
                break;
            }
            else if (StripeIndex >= StartPadStripIndex)
            {
                Array.Copy(buffer1, 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize1), writeSize1);
                totalBytesWritten += writeSize1;
            }

            _position += writeSize1;
            StripeIndex++;

            var writeSize2 = (int)Math.Min(_originalSize - _position, bytesRead2);

            if (StripeIndex == StartPadStripIndex)
            {
                writeSize2 = Math.Min(writeSize2, StartPaddingSize);
                if (StripeIndex == EndPadStripIndex)
                {
                    writeSize2 = EndPaddingSize - (totalBytesWritten % _stripeSize);

                    Array.Copy(buffer2, StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
                    totalBytesWritten += writeSize2;
                    _position += writeSize2;
                    StripeIndex++;
                    break;
                }

                Array.Copy(buffer2, StartPadding, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
            }
            else if (StripeIndex == EndPadStripIndex)
            {
                writeSize2 = EndPaddingSize;

                Array.Copy(buffer2, 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
                totalBytesWritten += writeSize2;
                _position += writeSize2;
                StripeIndex++;
                break;
            }
            else
            {
                Array.Copy(buffer2, 0, buffer, Math.Min(offset + totalBytesWritten, buffer.Length - writeSize2), writeSize2);
            }

            totalBytesWritten += writeSize2;
            _position += writeSize2;
            StripeIndex++;
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
            file1?.Dispose();
            file2?.Dispose();
            file3?.Dispose();
        }

        base.Dispose(disposing);
    }
}
namespace Business.Data;

public class Raid5Stream(RedundantArrayOfIndependentDisks redundantArrayOfIndependentDisks, string path, CancellationToken cancellationToken = default) : Stream
{
    private long currentPosition = 0;
    private long length;

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                currentPosition = offset;
                break;
            case SeekOrigin.Current:
                currentPosition += offset;
                break;
            case SeekOrigin.End:
                currentPosition = length + offset;
                break;
        }

        if (currentPosition < 0 || currentPosition >= length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek position is out of range.");
        }

        return currentPosition;
    }

    public override void SetLength(long value)
    {
        length = value;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
    {
        // Implement the read logic from RAID 5
        // Read data from the appropriate files based on currentPosition
        // Make sure to handle seeking logic within this method
        MemoryStream memoryStream = new MemoryStream();
        await redundantArrayOfIndependentDisks.ReadAndSeek(memoryStream, path, Position, cancel);
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.Write(buffer, offset, count);
        return 0; // Placeholder return
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // Implement the read logic from RAID 5
        // Read data from the appropriate files based on currentPosition
        // Make sure to handle seeking logic within this method
        MemoryStream memoryStream = new MemoryStream();
        redundantArrayOfIndependentDisks.ReadAndSeek(memoryStream, path, Position, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.Write(buffer, offset, count);
        return 0; // Placeholder return
    }

    // Implement other abstract members of the Stream class
    public override void Flush()
    {
        /* Implement flush */
    }

    public override long Length => length;

    public override long Position
    {
        get => currentPosition;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false; // If you want to make it read-only
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
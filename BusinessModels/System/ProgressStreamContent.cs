namespace BusinessModels.System;

public class ProgressStreamContent(Stream innerStream, IProgress<double> progress) : Stream
{
    private long _bytesRead = 0;
    private readonly long _totalBytes = innerStream.Length;

    public override bool CanRead => innerStream.CanRead;
    public override bool CanSeek => innerStream.CanSeek;
    public override bool CanWrite => innerStream.CanWrite;
    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override void Flush() => innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = innerStream.Read(buffer, offset, count);
        _bytesRead += bytesRead;

        // Report progress
        progress.Report((double)_bytesRead / _totalBytes * 100);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;

        // Report progress
        progress.Report((double)_bytesRead / _totalBytes * 100);
        return bytesRead;
    }
    
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        var bytesRead = await innerStream.ReadAsync(buffer, cancellationToken);
        _bytesRead += bytesRead;

        // Report progress
        progress.Report((double)_bytesRead / _totalBytes * 100);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
    public override void SetLength(long value) => innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);
}
namespace BusinessModels.Utils;

public static class StreamExtensions
{
    public static void SeekBeginOrigin(this Stream self)
    {
        if (self.CanSeek)
            self.Seek(0, SeekOrigin.Begin);
    }
}
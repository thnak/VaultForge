namespace BusinessModels.Utils;

public static class StreamExtensions
{
    /// <summary>
    /// seek when posible
    /// </summary>
    /// <param name="self"></param>
    public static void SeekBeginOrigin(this Stream self)
    {
        if (self.CanSeek)
            self.Seek(0, SeekOrigin.Begin);
    }
}
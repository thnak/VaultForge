namespace BusinessModels.Utils;

public static class StreamExtensions
{
    /// <summary>
    /// if it can be seeking. seek it first!
    /// </summary>
    /// <param name="self"></param>
    public static void SeekBeginOrigin(this Stream self)
    {
        if (self.CanSeek)
            self.Seek(0, SeekOrigin.Begin);
    }
}
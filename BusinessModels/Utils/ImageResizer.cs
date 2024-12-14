namespace BusinessModels.Utils;

public static class ImageResizer
{
    /// <summary>
    /// Resizes the image to the specified height while maintaining the aspect ratio.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="newHeight">The new height for the image.</param>
    /// <param name="height"></param>
    public static (int newHeight, int newWidth) ResizeByHeight(int height, int width, int newHeight)
    {
        double aspectRatio = (double)width / height;
        width = (int)(newHeight * aspectRatio);
        height = newHeight;
        return (height, width);
    }
}

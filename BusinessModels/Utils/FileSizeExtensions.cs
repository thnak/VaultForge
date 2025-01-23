namespace BusinessModels.Utils;

public static class FileSizeExtensions
{
    /// <summary>
    /// Converts a file size in bytes to a human-readable string.
    /// </summary>
    /// <param name="sizeInBytes">The file size in bytes.</param>
    /// <returns>A string representing the file size in appropriate units.</returns>
    public static string ToSizeString(this long sizeInBytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        return sizeInBytes switch
        {
            >= TB => $"{(sizeInBytes / (double)TB):F2} TB",
            >= GB => $"{(sizeInBytes / (double)GB):F2} GB",
            >= MB => $"{(sizeInBytes / (double)MB):F2} MB",
            >= KB => $"{(sizeInBytes / (double)KB):F2} KB",
            _ => $"{sizeInBytes} Bytes"
        };
    }
}
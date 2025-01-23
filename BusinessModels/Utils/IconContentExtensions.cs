using BusinessModels.General.EnumModel;
using BusinessModels.WebContent;

namespace BusinessModels.Utils;

public static class IconContentExtensions
{
    public static string GetIconContentType(this string self)
    {
        return self switch
        {
            nameof(FolderContentType.Folder) => "fa-solid fa-folder",
            nameof(FolderContentType.DeletedFolder) => "fa-solid fa-folder-xmark",
            nameof(FolderContentType.HiddenFolder) => "fa-solid fa-folder opacity-5",
            MimeTypeNames.Image.Jpeg => "fa-solid fa-image",
            MimeTypeNames.Image.Avif => "fa-solid fa-image",
            MimeTypeNames.Image.Bmp => "fa-solid fa-image",
            MimeTypeNames.Image.Gif => "fa-solid fa-image",
            MimeTypeNames.Image.Png => "fa-solid fa-image",
            MimeTypeNames.Image.Icon => "fa-solid fa-image",
            MimeTypeNames.Image.Svg => "fa-solid fa-image",
            MimeTypeNames.Image.Tiff => "fa-solid fa-image",
            MimeTypeNames.Image.Webp => "fa-solid fa-image",
            MimeTypeNames.Video.Avi => "fa-solid fa-video",
            MimeTypeNames.Video.Mkv => "fa-solid fa-video",
            MimeTypeNames.Video.Ts => "fa-solid fa-video",
            MimeTypeNames.Video.Mp4 => "fa-solid fa-video",
            MimeTypeNames.Video.Mpeg => "fa-solid fa-video",
            _ => "fa-solid fa-file-circle-question"
        };
    }

    /// <summary>
    /// Gets an icon string based on the file extension.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <returns>A string representing the icon (can be an HTML or Unicode character).</returns>
    public static string GetIconFromName(this string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "insert_drive_file"; // Default generic file icon

        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "image", // Image file icon
            ".pdf" => "picture_as_pdf", // PDF file icon
            ".doc" or ".docx" => "description", // Word document icon
            ".xls" or ".xlsx" => "table_chart", // Excel file icon
            ".ppt" or ".pptx" => "slideshow", // PowerPoint icon
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "folder_zip", // Archive file icon
            ".mp3" or ".wav" or ".aac" or ".flac" => "audiotrack", // Audio file icon
            ".mp4" or ".avi" or ".mov" or ".mkv" => "movie", // Video file icon
            ".txt" or ".log" => "text_snippet", // Text file icon
            ".html" or ".css" or ".js" or ".json" => "code", // Code file icon
            ".exe" => "settings", // Executable file icon
            _ => "insert_drive_file" // Default icon for unknown extensions
        };
    }
}
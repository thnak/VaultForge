using BusinessModels.System.FileSystem;
using BusinessModels.WebContent;

namespace WebApp.Client.Utils;

public static class IconContentExtensions
{
    public static string GetIcon(this string self)
    {
        return self switch
        {
            nameof(FolderContentType.Folder) => "fa-solid fa-folder",
            nameof(FolderContentType.DeletedFolder) => "fa-solid fa-folder-xmark",
            nameof(FolderContentType.HiddenFolder) => "fa-solid fa-folder opacity-5",
            nameof(FolderContentType.File) => "fa-solid fa-file",
            nameof(FolderContentType.HiddenFile) => "fa-solid fa-file opacity-5",
            nameof(FolderContentType.DeletedFile) => "fa-solid fa-file-excel",
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
}
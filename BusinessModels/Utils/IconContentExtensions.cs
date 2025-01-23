using BusinessModels.General.EnumModel;
using BusinessModels.WebContent;

namespace BusinessModels.Utils;

public static class IconContentExtensions
{
    public static string GetIconContentType(this string contentType, string fileName = "")
    {
        if (string.IsNullOrEmpty(contentType) || !string.IsNullOrEmpty(fileName) && contentType == "application/octet-stream")
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "fa-solid fa-image",
                ".avif" => "fa-solid fa-image",
                ".bmp" => "fa-solid fa-image",
                ".gif" => "fa-solid fa-image",
                ".png" => "fa-solid fa-image",
                ".ico" => "fa-solid fa-image",
                ".svg" => "fa-solid fa-image",
                ".tiff" => "fa-solid fa-image",
                ".webp" => "fa-solid fa-image",
                ".avi" => "fa-solid fa-video",
                ".mkv" => "fa-solid fa-video",
                ".ts" => "fa-solid fa-video",
                ".mp4" => "fa-solid fa-video",
                ".mpeg" => "fa-solid fa-video",
                ".onnx" or ".pth" or ".rknn" => "fa-solid fa-brain",
                _ => "fa-solid fa-file-circle-question"
            };
        }

        return contentType switch
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
}
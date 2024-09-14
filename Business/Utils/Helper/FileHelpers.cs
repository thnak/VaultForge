using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Business.Utils.Helper;

public static class FileHelpers
{
    // If you require a check on specific characters in the IsValidFileExtensionAndSignature
    // method, supply the characters in the _allowedChars field.
    private static readonly byte[] AllowedChars = [];

    // For more file signatures, see the File Signatures Database (https://www.filesignatures.net/)
    // and the official specifications for the file types you wish to add.
    private static readonly Dictionary<string, List<byte[]>> FileSignature = new()
    {
        { ".gif", ["GIF8"u8.ToArray()] },
        { ".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]] },
        { ".jpeg", [[0xFF, 0xD8, 0xFF, 0xE0], [0xFF, 0xD8, 0xFF, 0xE2], [0xFF, 0xD8, 0xFF, 0xE3]] },
        { ".jpg", [[0xFF, 0xD8, 0xFF, 0xE0], [0xFF, 0xD8, 0xFF, 0xE1], [0xFF, 0xD8, 0xFF, 0xE8]] },
        { ".webp", [[0x57, 0x45, 0x42, 0x50]] },
        {
            ".zip",
            [
                [0x50, 0x4B, 0x03, 0x04], "PKLITE"u8.ToArray(), "PKSpX"u8.ToArray(), [0x50, 0x4B, 0x05, 0x06],
                [0x50, 0x4B, 0x07, 0x08], "WinZip"u8.ToArray()
            ]
        },
        { ".bmp", [[0x42, 0x4D]] },
        { ".pdf", [[0x25, 0x50, 0x44, 0x46]] },
        { ".doc", [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]] },
        { ".xls", [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]] },
        { ".ppt", [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]] },
        { ".docx", [[0x50, 0x4B, 0x03, 0x04]] },
        {
            ".xlsx", [[0x50, 0x4B, 0x03, 0x04]]
        },
        {
            ".pptx", [[0x50, 0x4B, 0x03, 0x04]]
        },
        {
            ".tif", [
                [0x49, 0x49, 0x2A, 0x00],
                [0x4D, 0x4D, 0x00, 0x2A]
            ]
        },
        {
            ".tiff", [
                [0x49, 0x49, 0x2A, 0x00],
                [0x4D, 0x4D, 0x00, 0x2A]
            ]
        },

        { ".mpeg", [[0x00, 0x00, 0x01, 0xBA], [0x00, 0x00, 0x01, 0xB3]] },
        { ".mp4", [[0x00, 0x00, 0x01, 0xBA]] },
        { ".avi", [[0x52, 0x49, 0x46, 0x46]] },
        { ".wmv", [[0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C]] },
        { ".mp3", [[0x00, 0x00, 0x01, 0xBA]] },
        { ".wav", [[0x52, 0x49, 0x46, 0x46]] },
        { ".exe", [[0x4D, 0x5A]] },
        { ".xml", [[0x3C, 0x3F, 0x78, 0x6D, 0x6C]] },
        { ".html", [[0x68, 0x74, 0x6D, 0x6C, 0x3E]] },
        {
            ".flv", [[0x46, 0x4C, 0x56, 0x01]]
        },
        {
            ".mkv", [[0x1A, 0x45, 0xDF, 0xA3]]
        },
        {
            ".flac", [[0x66, 0x4C, 0x61, 0x43]]
        },
        {
            ".ogg", [[0x4F, 0x67, 0x67, 0x53]]
        },
        {
            ".wma", [[0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C]]
        },
        {
            ".json", [[0x7B]]
        },
        {
            ".csv", [[0xEF, 0xBB, 0xBF]]
        },
        {
            ".sqlite", [[0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66]]
        },
        {
            ".rtf", [[0x7B, 0x5C, 0x72, 0x74, 0x66, 0x31]]
        },
        {
            ".dll", [[0x4D, 0x5A]]
        },
        { ".ts", [[0x47]] }
    };

    private static readonly Dictionary<string, string> MimeTypeMappings = new()
    {
        // Text files
        { ".txt", "text/plain" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".csv", "text/csv" },
        { ".xml", "application/xml" },
        { ".json", "application/json" },

        // Image files
        { ".jpeg", "image/jpeg" },
        { ".jpg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },
        { ".ico", "image/x-icon" },
        { ".webp", "image/webp" },

        // Audio files
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".flac", "audio/flac" },

        // Video files
        { ".mp4", "video/mp4" },
        { ".mkv", "video/x-matroska" },
        { ".avi", "video/x-msvideo" },
        { ".mpeg", "video/mpeg" },
        { ".mpg", "video/mpeg" },
        { ".ts", "video/mp2t" },

        // Document files
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

        // Application files
        { ".exe", "application/octet-stream" },
        { ".dll", "application/octet-stream" },
        { ".zip", "application/zip" },
        { ".tar", "application/x-tar" },
        { ".rar", "application/x-rar-compressed" },
        { ".7z", "application/x-7z-compressed" },
        { ".gz", "application/gzip" },

        // Script files
        { ".js", "application/javascript" },
        { ".php", "application/x-httpd-php" },
        { ".sh", "application/x-sh" },
        { ".pl", "application/x-perl" },
        { ".py", "application/x-python-code" },
        { ".rb", "application/x-ruby" },
        { ".ps1", "application/x-powershell" },


        // Other
        { ".bin", "application/octet-stream" }
    };

    // **WARNING!**
    // In the following file processing methods, the file's content isn't scanned.
    // In most production scenarios, an antivirus/anti-malware scanner API is
    // used on the file before making the file available to users or other
    // systems. For more information, see the topic that accompanies this sample
    // app.

    public static async Task<byte[]> ProcessFormFile<T>(this IFormFile formFile, ModelStateDictionary modelState,
        string[] permittedExtensions, long sizeLimit)
    {
        var fieldDisplayName = string.Empty;

        // Use reflection to obtain the display name for the model
        // property associated with this IFormFile. If a display
        // name isn't found, error messages simply won't show
        // a display name.
        MemberInfo? property =
            typeof(T).GetProperty(formFile.Name.Substring(formFile.Name.IndexOf(".", StringComparison.Ordinal) + 1));

        if (property != null)
            if (property.GetCustomAttribute(typeof(DisplayAttribute)) is
                DisplayAttribute displayAttribute)
                fieldDisplayName = $"{displayAttribute.Name} ";

        // Don't trust the file name sent by the client. To display
        // the file name, HTML-encode the value.
        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
            formFile.FileName);

        // Check the file length. This check doesn't catch files that only have 
        // a BOM as their content.
        if (formFile.Length == 0)
        {
            modelState.AddModelError(formFile.Name, $@"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");

            return [];
        }

        if (formFile.Length > sizeLimit)
        {
            var megabyteSizeLimit = sizeLimit / 1048576;
            modelState.AddModelError(formFile.Name,
                $@"{fieldDisplayName}({trustedFileNameForDisplay}) exceeds " + $@"{megabyteSizeLimit:N1} MB.");

            return [];
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);

            // Check the content length in case the file's only
            // content was a BOM and the content is actually
            // empty after removing the BOM.
            if (memoryStream.Length == 0)
                modelState.AddModelError(formFile.Name, $@"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");

            if (!memoryStream.IsValidFileExtensionAndSignature(formFile.FileName, permittedExtensions))
                modelState.AddModelError(formFile.Name,
                    $@"{fieldDisplayName}({trustedFileNameForDisplay}) file type isn't permitted or the file's signature doesn't match the file's extension.");
            else
                return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            modelState.AddModelError(formFile.Name,
                $@"{fieldDisplayName}({trustedFileNameForDisplay}) upload failed. " +
                $@"Please contact the Help Desk for support. Error: {ex.HResult}");
            // Log the exception
        }

        return [];
    }

    public static async Task<byte[]> ProcessStreamedFile(this
            MultipartSection section, ContentDispositionHeaderValue contentDisposition,
        ModelStateDictionary modelState, string[] permittedExtensions, long sizeLimit)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await section.Body.CopyToAsync(memoryStream);

            // Check if the file is empty or exceeds the size limit.
            if (memoryStream.Length == 0)
            {
                modelState.AddModelError(AppLang.File, @"The file is empty.");
            }
            else if (memoryStream.Length > sizeLimit)
            {
                var megabyteSizeLimit = sizeLimit / 1048576;
                modelState.AddModelError(AppLang.File, $@"The file exceeds {megabyteSizeLimit:N1} MB.");
            }
            else if (permittedExtensions.Any())
            {
                var fileName = contentDisposition.FileName.Value;
                if (fileName == null || !memoryStream.IsValidFileExtensionAndSignature(fileName, permittedExtensions))
                    modelState.AddModelError(AppLang.File,
                        @"The file type isn't permitted or the file's signature doesn't match the file's extension.");
            }
            else
            {
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            modelState.AddModelError(AppLang.File,
                @"The upload failed. Please contact the Help Desk " + $@" for support. Error: {ex.Message}");
            // Log the exception
        }

        return [];
    }

    public static async Task<(long, string)> ProcessStreamedFileAndSave(this MultipartSection section, string path,
        ModelStateDictionary modelState, CancellationToken cancellationToken = default)
    {
        const int bufferSize = 10 * 1024 * 1024; // 80 KB buffer size (you can adjust this size based on performance needs)
        try
        {
            // Open target stream for writing the file to disk
            await using var targetStream = File.Create(path);
            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;

            // Stream the file in chunks from section.Body to disk, avoiding memory overload
            int bytesRead;
            while ((bytesRead = await section.Body.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await targetStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead; // Keep track of the total bytes read
            }

            // Determine file extension and MIME type (you can implement based on content type)
            var fileExtension = targetStream.GetCorrectExtension(section.ContentType);
            var contentType = fileExtension.GetMimeTypeFromExtension();

            // Return the total file size and MIME type
            return (totalBytesRead, contentType);
        }
        catch (OperationCanceledException ex)
        {
            modelState.AddModelError(AppLang.File, ex.Message);
            return (-1, ex.Message);
        }
        catch (Exception ex)
        {
            modelState.AddModelError(AppLang.File,
                @"The upload failed. Please contact the Help Desk " + $@" for support. Error: {ex.Message}");
            return (-1, string.Empty);
        }
    }


    private static bool IsValidFileExtensionAndSignature(this Stream? data, string fileName,
        params string[] permittedExtensions)
    {
        if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0) return false;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext)) return false;

        data.Position = 0;

        using var reader = new BinaryReader(data);
        if (ext.Equals(".txt") || ext.Equals(".csv") || ext.Equals(".prn"))
        {
            if (AllowedChars.Length == 0)
            {
                // Limits characters to ASCII encoding.
                for (var i = 0; i < data.Length; i++)
                    if (reader.ReadByte() > sbyte.MaxValue)
                        return false;
            }
            else
            {
                // Limits characters to ASCII encoding and
                // values of the _allowedChars array.
                for (var i = 0; i < data.Length; i++)
                {
                    var b = reader.ReadByte();
                    if (b > sbyte.MaxValue || !AllowedChars.Contains(b)) return false;
                }
            }

            return true;
        }

        // Uncomment the following code block if you must permit
        // files whose signature isn't provided in the _fileSignature
        // dictionary. We recommend that you add file signatures
        // for files (when possible) for all file types you intend
        // to allow on the system and perform the file signature
        // check.
        /*
            if (!_fileSignature.ContainsKey(ext))
            {
                return true;
            }
            */

        // File signature check
        // --------------------
        // With the file signatures provided in the _fileSignature
        // dictionary, the following code tests the input content's
        // file signature.
        var signatures = FileSignature[ext];
        var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

        return signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
    }

    public static string GetCorrectExtension(this Stream stream, string? defaultType = null)
    {
        try
        {
            stream.SeekBeginOrigin();
            using var reader = new BinaryReader(stream);
            stream.SeekBeginOrigin();

            var headerBytes = reader.ReadBytes(FileSignature.Values.SelectMany(x => x).Max(m => m.Length) * 2);

            foreach (var ext in FileSignature.Keys)
            {
                var signatures = FileSignature[ext];
                var result = signatures.Any(signature => IsSubArray(headerBytes, signature));
                if (result)
                {
                    return ext;
                }
            }

            return defaultType ?? string.Empty;
        }
        catch (Exception)
        {
            return defaultType ?? string.Empty;
        }
    }

    public static bool IsSubArray(byte[] largerArray, byte[] subArray)
    {
        if (subArray.Length == 0 || largerArray.Length < subArray.Length)
            return false;

        for (int i = 0; i <= largerArray.Length - subArray.Length; i++)
        {
            bool isMatch = true;
            for (int j = 0; j < subArray.Length; j++)
            {
                if (largerArray[i + j] != subArray[j])
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
                return true;
        }

        return false;
    }

    public static string GetCorrectExtensionFormContentType(this string contentType, string defaultExtension = ".bin")
    {
        return MimeTypeMappings.FirstOrDefault(x => x.Value == contentType).Key ?? defaultExtension;
    }

    public static string GetMimeTypeFromExtension(this string fileExtension)
    {
        var values = MimeTypeMappings.TryGetValue(fileExtension, out var type);
        if (values == false || type == null)
            return "application/octet-stream";
        return type;
    }

    public static bool IsImageFile(this string contentType)
    {
        string[] imageContentTypes = ["image/jpeg", "image/pjpeg", "image/gif", "image/x-png", "image/png", "image/bmp", "image/tiff"];
        return imageContentTypes.Contains(contentType);
    }
}
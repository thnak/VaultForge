using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
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
        { ".zip", [[0x50, 0x4B, 0x03, 0x04], "PKLITE"u8.ToArray(), "PKSpX"u8.ToArray(), [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08], "WinZip"u8.ToArray()] }
    };

    // **WARNING!**
    // In the following file processing methods, the file's content isn't scanned.
    // In most production scenarios, an antivirus/anti-malware scanner API is
    // used on the file before making the file available to users or other
    // systems. For more information, see the topic that accompanies this sample
    // app.

    public static async Task<byte[]> ProcessFormFile<T>(this IFormFile formFile, ModelStateDictionary modelState, string[] permittedExtensions, long sizeLimit)
    {
        var fieldDisplayName = string.Empty;

        // Use reflection to obtain the display name for the model
        // property associated with this IFormFile. If a display
        // name isn't found, error messages simply won't show
        // a display name.
        MemberInfo? property = typeof(T).GetProperty(formFile.Name.Substring(formFile.Name.IndexOf(".", StringComparison.Ordinal) + 1));

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
            if (memoryStream.Length == 0) modelState.AddModelError(formFile.Name, $@"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");

            if (!memoryStream.IsValidFileExtensionAndSignature(formFile.FileName, permittedExtensions))
                modelState.AddModelError(formFile.Name, $@"{fieldDisplayName}({trustedFileNameForDisplay}) file type isn't permitted or the file's signature doesn't match the file's extension.");
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
                modelState.AddModelError("File", @"The file is empty.");
            }
            else if (memoryStream.Length > sizeLimit)
            {
                var megabyteSizeLimit = sizeLimit / 1048576;
                modelState.AddModelError("File", $@"The file exceeds {megabyteSizeLimit:N1} MB.");
            }
            else if (permittedExtensions.Any())
            {
                var fileName = contentDisposition.FileName.Value;
                if (fileName == null || !memoryStream.IsValidFileExtensionAndSignature(fileName, permittedExtensions))
                {
                    modelState.AddModelError("File", @"The file type isn't permitted or the file's signature doesn't match the file's extension.");
                }
            }
            else
            {
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            modelState.AddModelError("File", @"The upload failed. Please contact the Help Desk " + $@" for support. Error: {ex.Message}");
            // Log the exception
        }

        return [];
    }

    public static async Task<long> ProcessStreamedFileAndSave(this MultipartSection section, string path, ContentDispositionHeaderValue contentDisposition,
        ModelStateDictionary modelState, long sizeLimit, params string[] permittedExtensions)
    {
        try
        {
            await using var targetStream = File.Create(path);
            await section.Body.CopyToAsync(targetStream);
            var length = new FileInfo(path).Length;
            return length;
        }
        catch (Exception ex)
        {
            modelState.AddModelError("File", @"The upload failed. Please contact the Help Desk " + $@" for support. Error: {ex.Message}");
            return 0;
        }
    }


    private static bool IsValidFileExtensionAndSignature(this Stream? data, string fileName, params string[] permittedExtensions)
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
}
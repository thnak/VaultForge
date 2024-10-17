using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = Microsoft.Net.Http.Headers.ContentDispositionHeaderValue;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace Business.Utils.Helper;

public static class MultipartRequestHelper
{
    /// <summary>
    ///     Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
    ///     The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
    /// </summary>
    /// <param name="contentType"></param>
    /// <param name="lengthLimit"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static string GetBoundary(this MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary)) throw new InvalidDataException("Missing content-type boundary.");

        if (boundary.Length > lengthLimit) throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");

        return boundary;
    }

    public static bool IsMultipartContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType) && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool HasFormDataContentDisposition(this ContentDispositionHeaderValue? contentDisposition)
    {
        // Content-Disposition: form-data; name="key";
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && string.IsNullOrEmpty(contentDisposition.FileName.Value)
               && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
    }

    public static bool HasFileContentDisposition(this ContentDispositionHeaderValue? contentDisposition)
    {
        // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
    }
    
    public static string? GetFileNameFromHeaders(this HttpContentHeaders headers)
    {
        // Check if the Content-Disposition header is present
        if (headers.ContentDisposition != null && !string.IsNullOrEmpty(headers.ContentDisposition.FileName))
        {
            // Remove any quotes from the file name
            return headers.ContentDisposition.FileName.Trim('"');
        }

        return null;
    }
    
    public static string GetFileNameFromUrl(this string url)
    {
        // Get the last part of the URL after the last '/'
        return Path.GetFileName(new Uri(url).AbsolutePath);
    }
}
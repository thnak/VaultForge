namespace Business.Models;

public static class FileSignatureValidator
{
    public static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] Gif87aSignature = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61];
    public static readonly byte[] Gif89aSignature = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    public static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46];

    public static readonly byte[] Mp4Signature = [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70];

    #region Validate special signature

    public static bool ValidateFileSignature(string filePath, byte[] expectedSignature)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] fileHeader = new byte[Math.Min(expectedSignature.Length, fileStream.Length)];
            fileStream.ReadExactly(fileHeader);

            return fileHeader.SequenceEqual(expectedSignature);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static (bool, byte[]) ValidateFileSignatureWithoutSeek(Stream stream, byte[] expectedSignature)
    {
        var bufferLength = Math.Min(stream.Length, expectedSignature.Length);
        byte[] fileHeader = new byte[bufferLength];
        try
        {
            stream.ReadExactly(fileHeader);
            return (fileHeader.SequenceEqual(expectedSignature), fileHeader);
        }
        catch (Exception)
        {
            return (false, fileHeader);
        }
    }

    public static bool ValidateFileSignatureAndSeek(Stream stream, byte[] expectedSignature)
    {
        var bufferLength = Math.Min(stream.Length, expectedSignature.Length);
        byte[] fileHeader = new byte[bufferLength];
        try
        {
            stream.ReadExactly(fileHeader);
            stream.Seek(0, SeekOrigin.Begin);
            return fileHeader.SequenceEqual(expectedSignature);
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion


    #region Validate image

    public static bool ValidateImageSignature(string path)
    {
        List<byte[]> imageSignatures = [JpegSignature, PngSignature, Gif89aSignature, Gif87aSignature];
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        foreach (var signature in imageSignatures)
        {
            if (ValidateFileSignatureAndSeek(fileStream, signature)) return true;
        }
        return false;
    }

    #endregion
}
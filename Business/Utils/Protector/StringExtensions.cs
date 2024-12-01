using System.Security.Cryptography;
using System.Text;

namespace Business.Utils.Protector;

public static class StringExtensions
{
    public static string ComputeSha256Hash(this string text)
    {
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
        var builder = new StringBuilder();
        foreach (var t in bytes)
            builder.Append(t.ToString("x2"));
        return builder.ToString();
    }
}
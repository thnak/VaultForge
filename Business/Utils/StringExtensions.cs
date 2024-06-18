using System.Text;
using System.Text.Json;

namespace Business.Utils;

public static class StringExtensions
{
    public static T? DecodeBase64String<T>(this string base64String)
    {
        var base64Bytes = Convert.FromBase64String(base64String);
        var plainText = Encoding.UTF8.GetString(base64Bytes);
        var json = JsonSerializer.Deserialize<T>(plainText);
        return json;
    }

    public static string DecodeBase64String(this string base64String)
    {
        if (string.IsNullOrEmpty(base64String)) return string.Empty;
        var base64Bytes = Convert.FromBase64String(base64String);
        var plainText = Encoding.UTF8.GetString(base64Bytes);
        return plainText;
    }

    public static string Encode2Base64String(this object model)
    {
        var plainText = JsonSerializer.Serialize(model);
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        return base64String;
    }

    public static string Encode2Base64String(this string message)
    {
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
        return base64String;
    }

    public static string AppendAndEncodeBase64StringAsUri(this string source, string message)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(source);
        if (!source.EndsWith('/')) stringBuilder.Append('/');
        var planText = Encode2Base64String(message);
        if (planText.Length < 2048)
            stringBuilder.Append(planText);
        return stringBuilder.ToString();
    }

    public static string ToBase64String(this byte[] data)
    {
        return Convert.ToBase64String(data);
    }
}
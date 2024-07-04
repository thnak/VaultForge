using System.Text;
using System.Text.Json;

namespace BusinessModels.Utils;

public static class StringExtension
{
    public static string AutoReplace(this string self, IEnumerable<string> text2Replace)
    {
        var index = 0;
        foreach (var text in text2Replace)
        {
            self = self.Replace($"{{{index}}}", text);
            index++;
        }
        return self;
    }
    public static T? DecodeBase64String<T>(this string base64String)
    {
        var base64Bytes = Convert.FromBase64String(base64String);
        var plainText = Encoding.Unicode.GetString(base64Bytes);
        var json = JsonSerializer.Deserialize<T>(plainText);
        return json;
    }
    public static string DecodeBase64String(this string base64String)
    {
        if (string.IsNullOrEmpty(base64String)) return string.Empty;
        var base64Bytes = Convert.FromBase64String(base64String);
        var plainText = Encoding.Unicode.GetString(base64Bytes);
        return plainText;
    }

    /// <summary>
    ///     Mã hóa base64 với unicode
    /// </summary>
    /// <param name="model"></param>
    /// <returns>string</returns>
    public static string Encode2Base64String(this object model)
    {
        var plainText = JsonSerializer.Serialize(model);
        var base64String = Convert.ToBase64String(Encoding.Unicode.GetBytes(plainText));
        return base64String;
    }

    public static string Encode2Base64String(this string message)
    {
        var base64String = Convert.ToBase64String(Encoding.Unicode.GetBytes(message));
        return base64String;
    }

    public static string AppendAndEncodeBase64StringAsUri(this string source, string message)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(source);
        if (!source.EndsWith('/')) stringBuilder.Append('/');
        stringBuilder.Append(Encode2Base64String(message));
        return stringBuilder.ToString();
    }
    public static string AppendAndEncodeBase64StringAsUri(this string source, params string[] message)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(source);
        if (!source.EndsWith('/')) stringBuilder.Append('/');
        foreach (var mess in message)
        {
            stringBuilder.Append(Encode2Base64String(mess));
            stringBuilder.Append('/');
        }
        return stringBuilder.ToString();
    }
}
using System.Text;
using System.Text.Json;
using System.Web;
using BusinessModels.Converter;

namespace BusinessModels.Utils;

public static class StringExtension
{
    private static readonly char[] InvalidCharPath = ['/', '\\', '*', '@', '%'];

    public static string AutoReplace(this string self, params string[] text2Replace)
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
        try
        {
            string plainText = DecodeBase64String(base64String);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)plainText;
            }

            var json = JsonSerializer.Deserialize<T>(plainText);
            return json;
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static string DecodeBase64String(this string base64String)
    {
        try
        {
            if (string.IsNullOrEmpty(base64String)) return string.Empty;
            var base64Bytes = Convert.FromBase64String(base64String);
            var plainText = Encoding.Unicode.GetString(base64Bytes);
            return plainText;
        }
        catch (Exception)
        {
            return string.Empty;
        }
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

    public static string UrlFriendlyEncode(this string self)
    {
        return HttpUtility.UrlEncode(self);
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
        stringBuilder.Append(HttpUtility.UrlEncode(Encode2Base64String(message)));
        return stringBuilder.ToString();
    }

    public static string ToJson(this object model)
    {
        return JsonSerializer.Serialize(model);
    }

    /// <summary>
    /// With description as key instead of Json name
    /// </summary>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string ToJson<T>(this object model) where T : class
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonDescriptionConverter<T>() },
            WriteIndented = true
        };
        return JsonSerializer.Serialize(model, options);
    }

    public static T? DeSerializeDescriptionJson<T>(this string source) where T : class
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonDescriptionConverter<T>() },
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<T>(source, options);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static T? DeSerialize<T>(this string self)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(self);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static T? DeSerialize<T>(this string self, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(self, options);
        }
        catch (Exception)
        {
            return default;
        }
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

    public static bool ValidateSystemPathName(this string self)
    {
        foreach (var c in self)
            if (Array.Exists(InvalidCharPath, invalidChar => invalidChar == c))
                return false;

        return true;
    }

    public static bool ValidateSystemPathName(this string self, out char? keyword)
    {
        foreach (var c in self)
            if (Array.Exists(InvalidCharPath, invalidChar => invalidChar == c))
            {
                keyword = c;
                return false;
            }

        keyword = default;
        return true;
    }
    
    public static bool IsImageFile(this string contentType)
    {
        string[] imageContentTypes = ["image/jpeg", "image/pjpeg", "image/gif", "image/x-png", "image/png", "image/bmp", "image/tiff", "image/webp", "image/tiff", "image/x-icon", "image/svg+xml", "image/bmp"];
        return imageContentTypes.Contains(contentType);
    }

    public static bool IsVideoFile(this string contenType)
    {
        string[] videoContentTypes = ["video/mp4"];
        return videoContentTypes.Contains(contenType);
    }
}
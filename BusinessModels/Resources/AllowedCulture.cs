using System.Globalization;

namespace BusinessModels.Resources;

public static class AllowedCulture
{
    public static IList<CultureInfo> SupportedCultures =>
    [
        new("vi-VN"), new("en-US"), new("Ja-JP"), new("ko-KR"), new("de-DE"), new("es-ES")
    ];
}
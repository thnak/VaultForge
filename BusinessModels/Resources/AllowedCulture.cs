using System.Globalization;

namespace BusinessModels.Resources;

public static class AllowedCulture
{
    public static IList<CultureInfo> SupportedCultures =>
        new[]
        {
            new CultureInfo("en-US"),
            new CultureInfo("vi-VN")
        };
}
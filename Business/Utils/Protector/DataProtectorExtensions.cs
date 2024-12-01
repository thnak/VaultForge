using Microsoft.AspNetCore.DataProtection;

namespace Business.Utils.Protector;

public static class DataProtectorExtensions
{
    public static string GenProtectedCode(this IDataProtector dataProtector, string code)
    {
        var protectedCode = dataProtector.Protect(code);
        return protectedCode;
    }
}
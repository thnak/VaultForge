namespace BusinessModels.Utils;

public static class TimeProviderExtensions
{
    public static DateTime Now(this TimeProvider timeProvider) => timeProvider.GetLocalNow().DateTime;
    public static string NowIsoString(this TimeProvider timeProvider) => timeProvider.Now().ToString("o");
    
    public static DateTime UtcNow(this TimeProvider timeProvider) => timeProvider.GetUtcNow().DateTime;
    public static string UtcNowIsoString(this TimeProvider timeProvider) => timeProvider.UtcNow().ToString("o");
}
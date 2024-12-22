namespace BusinessModels.Utils;

public static class DataTimeExtensions
{
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly UnixDateEpoch = DateOnly.FromDateTime(UnixEpoch);

    public static DateTime CloneWithSpecificTimestamp(this DateTime dateTime, int hour, int second)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, second, 0, dateTime.Kind);
    }

    public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp)
    {
        return UnixEpoch.AddSeconds(unixTimeStamp);
    }

    public static DateTime UnixTimeStampToDateTime(this int unixTimeStamp)
    {
        return UnixEpoch.AddSeconds(unixTimeStamp);
    }

    public static DateOnly UnixDate2DateOnly(this int date)
    {
        return UnixDateEpoch.AddDays(date);
    }

    public static DateTime UnixDate2DateDateTime(this double date)
    {
        return UnixEpoch.AddDays(date);
    }
    
    public static DateTime UnixDate2DateDateTime(this int date)
    {
        return UnixEpoch.AddDays(date);
    }

    public static int GetUnixDate(this DateOnly date)
    {
        var time = date.ToDateTime(TimeOnly.MinValue) - UnixEpoch;
        return (int)time.TotalDays;
    }

    public static double ToUnixSecond(this DateTime self)
    {
        var dateTimeOffset = new DateTimeOffset(self);
        var min = new DateTimeOffset(UnixEpoch);
        var span = dateTimeOffset - min;
        return span.TotalSeconds;
    }
    
    public static double ToUnixDate(this DateTime self)
    {
        var dateTimeOffset = new DateTimeOffset(self);
        var min = new DateTimeOffset(UnixEpoch);
        var span = dateTimeOffset - min;
        return span.TotalDays;
    }
}
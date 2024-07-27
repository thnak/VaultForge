namespace BusinessModels.Utils;

public static class DataTimeExtensions
{
    public static DateTime CloneWithSpecificTimestamp(this DateTime dateTime, int hour, int second)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, second, 0, dateTime.Kind);
    }

    public static double ToUnixSecond(this DateTime self)
    {
        var dateTimeOffset = new DateTimeOffset(self);
        var min = new DateTimeOffset(new DateTime(1970, 0, 0));
        var span = dateTimeOffset - min;
        return span.TotalSeconds;
    }
}
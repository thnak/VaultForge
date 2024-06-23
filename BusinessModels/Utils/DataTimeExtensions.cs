namespace BusinessModels.Utils;

public static class DataTimeExtensions
{
    public static DateTime CloneWithSpecificTimestamp(this DateTime dateTime, int hour, int second)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, second, 0, dateTime.Kind);
    }
}
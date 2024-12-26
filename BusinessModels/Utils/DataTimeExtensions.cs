namespace BusinessModels.Utils;

public static class DataTimeExtensions
{
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly UnixDateEpoch = DateOnly.FromDateTime(UnixEpoch);

    public static DateTime CloneWithSpecificTimestamp(this DateTime dateTime, int hour, int second)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, second, 0, dateTime.Kind);
    }

    public static DateTime UnixSecondToDateTime(this double unixTimeStamp)
    {
        return UnixEpoch.AddSeconds(unixTimeStamp);
    }

    public static DateTime UnixSecondToDateTime(this int unixTimeStamp)
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
        var span = self.ToUniversalTime() - UnixEpoch;
        return span.TotalSeconds;
    }

    /// <summary>
    /// Converts a DateTime to a Unix date (number of seconds since January 1, 1970).
    /// The time part is stripped, and the calculation is based on the date only.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The Unix date as an integer.</returns>
    public static long ToUnixDate(this DateTime dateTime)
    {
        // Strip the time part by creating a DateOnly and then converting it back to DateTime.
        var dateOnly = DateOnly.FromDateTime(dateTime);
        var strippedDateTime = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        
        // Calculate the total seconds since the Unix epoch.
        return (long)(strippedDateTime - UnixEpoch).TotalDays;
    }

    /// <summary>
    /// Converts a Unix date (seconds since January 1, 1970) back to a DateTime.
    /// </summary>
    /// <param name="unixDate">The Unix date to convert.</param>
    /// <returns>The corresponding DateTime.</returns>
    public static DateTime FromUnixDate(long unixDate)
    {
        // Add the Unix date as seconds to the epoch and return the result.
        return UnixEpoch.AddDays(unixDate);
    }
}
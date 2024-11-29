namespace BusinessModels.System.InternetOfThings.status;

public enum IoTSensorStatus
{
    Operational,
    Maintenance,
    Offline,
    /// <summary>
    /// For sensors with detected issues
    /// </summary>
    Faulty
}
namespace BusinessModels.System.InternetOfThings.status;

public enum IoTDeviceStatus
{
    Active,
    Inactive,
    Maintenance,
    Offline,
    /// <summary>
    /// Optional for retired devices
    /// </summary>
    Decommissioned
}
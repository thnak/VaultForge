namespace BusinessModels.System.InternetOfThings;

public class RequestToCreate
{
    public required IoTDevice Device { get; set; }
    public List<IoTSensor> Sensors { get; set; } = [];
}
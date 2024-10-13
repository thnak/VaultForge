using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTRecord
{
    [BsonId] public ObjectId Id { get; set; }

    /// <summary>
    /// The timestamp for when the data was recorded
    /// </summary>
    [BsonElement("timestamp")] public DateTime Timestamp { get; set; }

    /// <summary>
    /// Sensor or device identifier
    /// </summary>
    [BsonElement("deviceId")] public string DeviceId { get; set; }

    /// <summary>
    /// Sensor data (could be temperature, humidity, etc.)
    /// </summary>
    [BsonElement("sensorData")] public double SensorData { get; set; }

    /// <summary>
    /// What kind of sensor this is (e.g., temperature, humidity)
    /// </summary>
    [BsonElement("sensorType")]
    public SensorType SensorType { get; set; }
    
    public DateTime Date { get; set; }

    public int Hour { get; set; }

    /// <summary>
    /// Optional metadata
    /// </summary>
    [BsonElement("metadata")] public DeviceMetadata? Metadata { get; set; }

    public IoTRecord(string deviceId, double sensorData, DeviceMetadata? metadata = null)
    {
        Id = ObjectId.GenerateNewId();
        Timestamp = DateTime.UtcNow; // Set to current time by default
        DeviceId = deviceId;
        SensorData = sensorData;
        Metadata = metadata;
        Date = Timestamp.Date;
        Hour = Timestamp.Hour;
    }
}

public class DeviceMetadata
{
    /// <summary>
    /// Represented as a percentage (0-100)
    /// </summary>
    [BsonElement("signalStrength")]
    public int SignalStrength { get; set; }

    /// <summary>
    /// Represented as a percentage (0-100)
    /// </summary>
    [BsonElement("batteryLevel")]
    public int BatteryLevel { get; set; }

    /// <summary>
    /// Optional, where the device is located
    /// </summary>
    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;
    
    public string Manufacturer { get; set; } = string.Empty;

    
    public DateTimeOffset LastCalibration { get; set; }

    /// <summary>
    /// Optional, current firmware version
    /// </summary>
    [BsonElement("firmwareVersion")]
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// Optional, when the device was last serviced or maintained
    /// </summary>
    [BsonElement("lastServiceDate")]
    public DateTime LastServiceDate { get; set; }
}

public enum SensorType
{
    Temperature,
    Humidity,
    Pressure,
    Light,
    Proximity,
    Accelerometer,
    Gyroscope,
    Magnetometer,
    HeartRate,
    GPS,
    PingStatus
}
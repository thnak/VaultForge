using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTRecord
{
    [BsonId] public ObjectId Id { get; set; }

    // The timestamp for when the data was recorded
    [BsonElement("timestamp")] public DateTime Timestamp { get; set; }

    // Sensor or device identifier
    [BsonElement("deviceId")] public string DeviceId { get; set; }

    // Sensor data (could be temperature, humidity, etc.)
    [BsonElement("sensorData")] public double SensorData { get; set; }

    // Optional metadata
    [BsonElement("metadata")] public DeviceMetadata? Metadata { get; set; }

    public IoTRecord(string deviceId, double sensorData, DeviceMetadata? metadata = null)
    {
        Id = ObjectId.GenerateNewId();
        Timestamp = DateTime.UtcNow; // Set to current time by default
        DeviceId = deviceId;
        SensorData = sensorData;
        Metadata = metadata;
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

    /// <summary>
    /// What kind of sensor this is (e.g., temperature, humidity)
    /// </summary>
    [BsonElement("sensorType")]
    public string SensorType { get; set; } = string.Empty;

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
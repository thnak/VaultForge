using BusinessModels.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTRecord : BaseModelEntry
{
    /// <summary>
    /// The timestamp for when the data was recorded
    /// </summary>
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Sensor or device identifier
    /// </summary>
    [BsonElement("sensorId")]
    public string SensorId { get; set; }
    
    /// <summary>
    /// Sensor data (could be temperature, humidity, etc.)
    /// </summary>
    [BsonElement("sensorData")]
    public float SensorData { get; set; }


    [BsonDateTimeOptions(DateOnly = true, Kind = DateTimeKind.Utc)]
    public DateTime Date { get; set; }

    public int Hour { get; set; }

    /// <summary>
    /// Optional metadata
    /// </summary>
    [BsonElement("metadata")]
    public DeviceMetadata? Metadata { get; set; }

    public IoTRecord( string sensorId, float sensorData, DeviceMetadata? metadata = null)
    {
        Id = ObjectId.GenerateNewId();
        Timestamp = DateTime.UtcNow; // Set to current time by default
        SensorId = sensorId;
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
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastServiceDate { get; set; }
}
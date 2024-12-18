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

    [BsonDateTimeOptions(DateOnly = true, Kind = DateTimeKind.Utc)]
    public DateTime Date { get; set; }

    public int Hour { get; set; }

    /// <summary>
    /// Optional metadata
    /// </summary>
    [BsonElement("metadata")]
    public RecordMetadata Metadata { get; set; }

    public IoTRecord(RecordMetadata metadata)
    {
        Id = ObjectId.GenerateNewId();
        Timestamp = DateTime.UtcNow; // Set to current time by default
        Metadata = metadata;
        Date = Timestamp.Date;
        Hour = Timestamp.Hour;
    }
}

public class RecordMetadata
{
    /// <summary>
    /// Sensor or device identifier
    /// </summary>
    [BsonElement("sensorId")]
    public string SensorId { get; set; } = string.Empty;

    /// <summary>
    /// Sensor data (could be temperature, humidity, etc.)
    /// </summary>
    [BsonElement("sensorData")]
    public float SensorData { get; set; }

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

    [BsonElement("image")] public string ImagePath { get; set; } = string.Empty;
}
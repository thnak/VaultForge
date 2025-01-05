using BusinessModels.Base;
using BusinessModels.System.InternetOfThings.type;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTRecord : BaseModelEntry
{
    [BsonDateTimeOptions(DateOnly = true, Kind = DateTimeKind.Utc)]
    public DateTime Date { get; set; }
    public int Hour { get; set; }
    
    public RecordMetadata Metadata { get; set; }
    public IoTRecord(RecordMetadata metadata)
    {
        CreateTime = DateTime.UtcNow; // Set to current time by default
        Metadata = metadata;
        Date = CreateTime.Date;
        Hour = CreateTime.Hour;
    }

    // Default constructor for new instances
    public IoTRecord()
    {
        Metadata = new RecordMetadata();
        CreateTime = DateTime.UtcNow; // Set to current time by default
        Date = CreateTime.Date;
        Hour = CreateTime.Hour;
    }

    // Constructor for deserialization
    [BsonConstructor]
    public IoTRecord(ObjectId id, RecordMetadata metadata) : base(id)
    {
        Metadata = metadata;
        CreateTime = DateTime.UtcNow; // Set to current time by default
        Date = CreateTime.Date;
        Hour = CreateTime.Hour;
    }
}

public class RecordMetadata
{
    /// <summary>
    /// Sensor or device identifier
    /// </summary>
    public string SensorId { get; set; } = string.Empty;
    /// <summary>
    /// Sensor data (could be temperature, humidity, etc.)
    /// </summary>
    public float SensorData { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow; // Copy of Timestamp
    /// <summary>
    /// Represented as a percentage (0-100)
    /// </summary>
    public int SignalStrength { get; set; }
    public ProcessStatus ProcessStatus { get; set; }
    public float OnChipTemperature { get; set; }
    /// <summary>
    /// Represented as a percentage (0-100)
    /// </summary>
    public int BatteryLevel { get; set; }
    public string ImagePath { get; set; } = string.Empty;
}

public class IoTRecordUpdateModel
{
    public string SensorId { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public float SensorData { get; set; }
    public ProcessStatus ProcessStatus { get; set; }
}
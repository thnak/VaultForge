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
    [BsonElement("metadata")] public BsonDocument Metadata { get; set; }

    public IoTRecord(string deviceId, double sensorData, BsonDocument? metadata = null)
    {
        Id = ObjectId.GenerateNewId();
        Timestamp = DateTime.UtcNow; // Set to current time by default
        DeviceId = deviceId;
        SensorData = sensorData;
        Metadata = metadata ?? new BsonDocument();
    }
}
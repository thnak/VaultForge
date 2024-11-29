using BusinessModels.Base;
using BusinessModels.System.InternetOfThings.status;
using BusinessModels.System.InternetOfThings.type;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTSensor : BaseModelEntry
{
    [BsonElement("sensorId")] public string SensorId { get; set; } = string.Empty;

    [BsonElement("sensorName")] public string SensorName { get; set; } = string.Empty;

    [BsonElement("sensorType")]
    [BsonRepresentation(BsonType.String)]
    public IoTSensorType IoTSensorType { get; set; } // Enum for sensor types

    [BsonElement("deviceId")] public ObjectId DeviceId { get; set; } // Foreign key to Device

    [BsonElement("unitOfMeasurement")] public string UnitOfMeasurement { get; set; } = string.Empty; // e.g., °C, %, Pa

    [BsonElement("accuracy")] public float Accuracy { get; set; }

    [BsonElement("calibrationTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CalibrationTime { get; set; }

    [BsonElement("isActive")] public bool IsActive { get; set; } = true;

    public IoTSensorStatus Status { get; set; }
}
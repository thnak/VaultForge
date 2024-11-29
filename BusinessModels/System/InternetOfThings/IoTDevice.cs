using BusinessModels.Base;
using BusinessModels.System.InternetOfThings.status;
using BusinessModels.System.InternetOfThings.type;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTDevice : BaseModelEntry
{
    [BsonElement("deviceId")] public string DeviceId { get; set; } = string.Empty;

    [BsonElement("deviceName")] public string DeviceName { get; set; } = string.Empty;

    [BsonElement("group")] public ObjectId DeviceGroup { get; set; }

    [BsonElement("deviceType")]
    [BsonRepresentation(BsonType.String)]
    public IoTDeviceType IoTDeviceType { get; set; } // e.g., Gateway, Sensor Node

    [BsonElement("manufacturer")] public string Manufacturer { get; set; } = string.Empty;

    [BsonElement("installationDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime InstallationDate { get; set; }

    [BsonElement("location")] public string Location { get; set; } = string.Empty;

    [BsonElement("firmwareVersion")] public string FirmwareVersion { get; set; } = string.Empty;

    [BsonElement("lastServiceDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastServiceDate { get; set; }
    
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public IoTDeviceStatus Status { get; set; }

    [BsonElement("macAddress")] public string MacAddress { get; set; } = string.Empty;

    [BsonElement("ipAddress")] public string IpAddress { get; set; } = string.Empty;
}
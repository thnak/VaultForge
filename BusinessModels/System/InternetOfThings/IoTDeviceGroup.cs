using BusinessModels.Base;
using BusinessModels.System.InternetOfThings.status;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTDeviceGroup : BaseModelEntry
{
    [BsonElement("groupId")] public string GroupId { get; set; } = string.Empty;
    [BsonElement("groupName")] public string GroupName { get; set; } = string.Empty;

    [BsonElement("description")] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Helps track where the group is deployed (e.g., a specific building, city, or country).
    /// </summary>
    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("deploymentDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DeploymentDate { get; set; }

    [BsonElement("deployedBy")] public string DeployedBy { get; set; } = string.Empty; // e.g., "John Doe"

    [BsonElement("status")] 
    public IoTDeviceGroupStatus Status { get; set; }

    [BsonElement("accessLevel")] public AccessLevel AccessLevel { get; set; }

    [BsonElement("tags")] public List<string> Tags { get; set; } = new List<string>();

    [BsonElement("totalDevices")]
    [BsonIgnore]
    public int TotalDevices { get; set; }

    [BsonElement("onlineDevices")]
    [BsonIgnore]
    public int OnlineDevices { get; set; }
}
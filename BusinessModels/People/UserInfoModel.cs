using System.Security.Claims;
using System.Text.Json.Serialization;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

[MessagePackObject]
public class UserInfoModel
{
    public UserInfoModel()
    {
    }

    public UserInfoModel(UserModel user) : this(user, string.Empty)
    {
    }

    public UserInfoModel(UserModel user, string token)
    {
        LastLogin = user.LastLogin;
        PhoneNumber = user.PhoneNumber;
        Email = user.Email;
        UserName = user.UserName;
        Roles = user.Roles;
        LastConnect = user.LastConnect;
        Token = token;
        Avatar = user.Avatar;
    }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(0)]
    [BsonElement("0")]
    public DateTime LastLogin { get; set; }

    [BsonElement("1")] [Key(1)] public string PhoneNumber { get; set; } = string.Empty;
    [BsonElement("2")] [Key(2)] public string Department { get; set; } = string.Empty;
    [BsonElement("3")] [Key(3)] public string Email { get; set; } = string.Empty;
    [BsonElement("4")] [Key(4)] public string Company { get; set; } = string.Empty;
    [BsonElement("5")] [Key(5)] public string UserName { get; set; } = "Anonymous";
    [BsonElement("6")] [Key(6)] public List<string> Roles { get; set; } = new();
    [BsonElement("7")] [Key(7)] public List<string> RoleGroups { get; set; } = new();

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(8)]
    [BsonElement("8")]
    public DateTime LastConnect { get; set; }

    [BsonElement("9")] [Key(9)] public string Token { get; set; } = string.Empty;
    [BsonElement("10")] [Key(10)] public string Avatar { get; set; } = "default_user.jpg";

    [Key(11)]
    [JsonIgnore]
    [BsonIgnore]
    [IgnoreMember]
    public List<Claim> Claims { get; set; } = new();

    /// <summary>
    ///     key: Ip address
    ///     Values: signalr connection id
    /// </summary>
    [Key(12)]
    [BsonElement("12")]
    public Dictionary<string, List<string>> Connections { get; set; } = new();

    [Key(13)] public string JwtAccessToken { get; set; } = string.Empty;
}
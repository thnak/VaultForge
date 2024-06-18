using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

[MessagePackObject]
public class UserModel
{
    [BsonId]
    [Key(0)]
    public ObjectId ObjectId { get; set; } = ObjectId.GenerateNewId();
    [Key(1)]
    public string UserName { get; set; } = string.Empty;
    [Key(2)]
    public string FullName { get; set; } = string.Empty;
    [Key(3)]
    public string Alias { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(4)]
    public DateTime BirthDay { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(5)]
    public DateTime JoinDate { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(6)]
    public DateTime Leave { get; set; }

    [Key(7)]
    public string Email { get; set; } = string.Empty;
    [Key(8)]
    public string PasswordHash { get; set; } = string.Empty;
    [Key(9)]
    public string PhoneNumber { get; set; } = string.Empty;
    [Key(10)]
    public int AccessFailedCount { get; set; }
    [Key(11)]
    public bool AuthenticatorKey { get; set; }
    [Key(12)]
    public List<string> Roles { get; set; } = new();
    [Key(13)]
    public List<string> RoleGroups { get; set; } = new();
    [Key(14)]
    public List<string> Tokens { get; set; } = new();
    [Key(15)]
    public string Department { get; set; } = string.Empty;
    [Key(16)]
    public string Company { get; set; } = string.Empty;
    [Key(17)]
    public string SecurityStamp { get; set; } = string.Empty;//This is stamp for change User/Password.

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(18)]
    public DateTime LastConnect { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(19)]
    public DateTime LastLogin { get; set; }

    [Key(20)]
    public string ImageUrl { get; set; } = "default_user.jpg";
    [Key(21)]
    public string QuickLoginKey { get; set; } = Guid.NewGuid().ToString();

    [BsonIgnore] [Key(22)] public string EmailConfirmed { get; set; } = string.Empty;
}
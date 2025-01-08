using BusinessModels.Attribute;
using BusinessModels.Base;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

[MessagePackObject]
public class UserModel : BaseModelEntry
{
    [Key(3)]
    [IndexedDbKey]
    public string UserName { get; set; } = string.Empty;

    [Key(4)]
    public string Password { get; set; } = string.Empty;

    [Key(5)]
    public string FullName { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(6)]
    public DateTime BirthDay { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(7)]
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(8)]
    public DateTime Leave { get; set; }

    [Key(9)]
    public string Email { get; set; } = string.Empty;


    [Key(10)]
    public string PhoneNumber { get; set; } = string.Empty;


    [Key(11)]
    public List<string> Roles { get; set; } = new();


    [Key(12)]
    public List<string> Tokens { get; set; } = [];

    /// <summary>
    ///     Tổng số lần sai mật khẩu
    /// </summary>
    [Key(13)]
    public int AccessFailedCount { get; set; }

    /// <summary>
    ///     Số lần sai mật khẩu hiện tại
    /// </summary>
    [Key(14)]
    public int CurrentFailCount { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(15)]
    public DateTime BanTime { get; set; }

    [Key(16)]
    public bool AuthenticatorKey { get; set; }

    [Key(17)]
    public string SecurityStamp { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(18)]
    public DateTime LastConnect { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(19)]
    public DateTime LastLogin { get; set; }

    [Key(20)]
    public string Avatar { get; set; } = "default_user.jpg";
}
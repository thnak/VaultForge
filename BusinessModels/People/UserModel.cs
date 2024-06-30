using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

[MessagePackObject]
public class UserModel
{
    [BsonId]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key(0)]
    public ObjectId ObjectId { get; set; } = ObjectId.GenerateNewId();
    [Key(1)]
    public string UserName { get; set; } = string.Empty;
    [Key(19)]
    public string Password { get; set; } = string.Empty;

    [Key(2)]
    public string FullName { get; set; } = string.Empty;

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
    
    
    [Key(9)]
    public string PhoneNumber { get; set; } = string.Empty;


    [Key(10)]
    public List<string> Roles { get; set; } = new();


    [Key(11)]
    public List<string> Tokens { get; set; } = new();

    /// <summary>
    /// Tổng số lần sai mật khẩu
    /// </summary>
    [Key(12)]
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// Số lần sai mật khẩu hiện tại
    /// </summary>
    [Key(20)]
    public int CurrentFailCount { get; set; }
    
    [Key(13)]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BanTime { get; set; }

    [Key(14)]
    public bool AuthenticatorKey { get; set; }

    [Key(15)]
    public string SecurityStamp { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(16)]
    public DateTime LastConnect { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(17)]
    public DateTime LastLogin { get; set; }


    [Key(18)]
    public string Avatar { get; set; } = "default_user.jpg";
}
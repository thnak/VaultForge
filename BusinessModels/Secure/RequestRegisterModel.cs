using System.ComponentModel.DataAnnotations;
using BusinessModels.Resources;
using MessagePack;

namespace BusinessModels.Secure;

[MessagePackObject]
public class RequestRegisterModel
{
    [Required]
    [MessagePack.Key(0)]
    public string Username { get; set; } = string.Empty;
    [MessagePack.Key(1)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.", ErrorMessageResourceType = typeof(AppLang))]
    [MessagePack.Key(2)]
    public string ValidatePassword { get; set; } = string.Empty;

    [MessagePack.Key(3)]
    public string FullName { get; set; } = string.Empty;
    [MessagePack.Key(4)]
    public string Culture { get; set; } = string.Empty;

    [MessagePack.Key(5)]
    public DateTime BirthDay { get; set; }

    [MessagePack.Key(6)]
    public string Email { get; set; } = string.Empty;
}
using MessagePack;

namespace BusinessModels.Secure;

[MessagePackObject]
public class RequestLoginModel
{
    [Key(0)]
    public string UserName { get; set; } = string.Empty;
    [Key(1)]
    public string Password { get; set; } = string.Empty;
    [Key(2)]
    public string Message { get; set; } = string.Empty;
    [Key(3)]
    public string ReturnUrl { get; set; } = string.Empty;
}
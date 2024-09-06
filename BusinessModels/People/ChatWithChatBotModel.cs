namespace BusinessModels.People;

public class ChatWithChatBotModel
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string[] Images { get; set; } = [];
    public string ToolCalls { get; set; } = string.Empty;
}
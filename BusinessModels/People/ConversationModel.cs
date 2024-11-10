using BusinessModels.Base;

namespace BusinessModels.People;

public class ConversationModel : BaseModelEntry
{
    public string UserName { get; set; } = string.Empty;
    public string ConversationName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
}
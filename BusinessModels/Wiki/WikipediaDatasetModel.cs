using BusinessModels.Base;

namespace BusinessModels.Wiki;

public class WikipediaDatasetModel : BaseModelEntry
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Pages.DefaultPages;

public partial class NotFoundPage : ComponentBase
{
    private List<Dictionary<string, string>> Metadata { get; set; } = [];

    protected override void OnInitialized()
    {
        Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", "" } });
        base.OnInitialized();
    }
}
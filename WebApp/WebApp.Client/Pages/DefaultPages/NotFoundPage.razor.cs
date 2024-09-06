using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Pages.DefaultPages;

public partial class NotFoundPage : ComponentBase
{
    private List<Dictionary<string, string>> Metadata { get; set; } = [];
    private string Title { get; set; } = string.Empty;
    protected override void OnInitialized()
    {
        Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", AppLang.The_page_you_were_looking_for_does_not_exist__Don_t_worry__it_happens_to_the_best_of_us_ } });
        Metadata.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", "404 Not Found" } });
        Metadata.Add(new Dictionary<string, string>() { { "name", "robots" }, { "content", "index, follow" } });
        Title = "404 Not Found";
        base.OnInitialized();
    }
}
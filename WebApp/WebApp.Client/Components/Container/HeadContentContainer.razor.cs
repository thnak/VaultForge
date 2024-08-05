using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Components.Container;

public partial class HeadContentContainer : ComponentBase
{
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public string Uri { get; set; } = string.Empty;
}
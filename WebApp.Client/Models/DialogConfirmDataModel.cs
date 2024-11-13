using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Models;

public class DialogConfirmDataModel
{
    public string Icon { get; set; } = string.Empty;
    public Color Color { get; set; } = Color.Default;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public RenderFragment? Fragment { get; set; }
}
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Models;

public class DialogConfirmDataModel
{
    public string TitleIcon { get; set; } = string.Empty;
    public Color Color { get; set; } = Color.Default;
    public string Message { get; set; } = string.Empty;

    public RenderFragment? Fragment { get; set; }
}
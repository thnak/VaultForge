using MudBlazor;

namespace WebApp.Client.Models;

public class ButtonAction
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Size ButtonSize { get; set; } = Size.Small;
    public Variant ButtonVariant { get; set; } = Variant.Text;
    public Color ButtonColor { get; set; } = Color.Primary;
    public Action Action { get; set; } = () => { };
    public bool Disabled { get; set; }
}
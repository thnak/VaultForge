namespace WebApp.Client.Models;

public class ButtonAction
{
    public string Title { get; set; } = string.Empty;
    public Action Action { get; set; } = () => { };
    public bool Disabled { get; set; }
}
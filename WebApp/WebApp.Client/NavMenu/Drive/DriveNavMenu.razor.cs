using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.NavMenu.Drive;

public partial class DriveNavMenu : ComponentBase
{
    private string DeletedShared { get; set; } = string.Empty;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            DeletedShared = Navigation.GetUriWithQueryParameters(PageRoutes.Drive.Trash.Src, new Dictionary<string, object?>() { { "status", "deleted" } });
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
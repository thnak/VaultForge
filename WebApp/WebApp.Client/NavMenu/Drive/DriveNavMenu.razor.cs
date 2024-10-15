using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.NavMenu.Drive;

public partial class DriveNavMenu : ComponentBase
{
    private string DeletedShared { get; set; } = string.Empty;
    private string DeletedPrivate { get; set; } = string.Empty;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            var deletedParam = new Dictionary<string, object?>() { { "status", "deleted" } };
            DeletedPrivate = Navigation.GetUriWithQueryParameters(PageRoutes.Drive.Index.Src, deletedParam);
            DeletedShared = Navigation.GetUriWithQueryParameters(PageRoutes.Drive.Shared.Src, deletedParam);
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
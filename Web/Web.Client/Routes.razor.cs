using Microsoft.AspNetCore.Components;

namespace Web.Client;

public partial class Routes : ComponentBase, IDisposable
{
    public void Dispose()
    {
        CustomStateContainer.OnChangedAsync -= OnChangedAsync;
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            CustomStateContainer.OnChangedAsync += OnChangedAsync;
        }
        base.OnAfterRender(firstRender);
    }
    private async Task OnChangedAsync()
    {
        await InvokeAsync(StateHasChanged);
    }
}
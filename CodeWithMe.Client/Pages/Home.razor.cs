using Microsoft.AspNetCore.Components;

namespace CodeWithMe.Client.Pages;

public partial class Home(PersistentComponentState state) : ComponentBase, IDisposable
{
    private PersistingComponentStateSubscription _subscription;

    protected override async Task OnInitializedAsync()
    {
        if (state.TryTakeFromJson<string>("myData", out string? instance))
        {
            Console.WriteLine(instance);
        }
        _subscription = state.RegisterOnPersisting(Persist);

        await base.OnInitializedAsync();
    }

    private Task Persist()
    {
        state.PersistAsJson("myData", "Hello world!");
        return Task.CompletedTask;
    }
    public void Dispose()
    {
        _subscription.Dispose();
    }
}
using BusinessModels.General;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebApp.Client.Components;

public partial class LabelDrawerContainer : ComponentBase, IDisposable
{
    private readonly string _containerId = Guid.NewGuid().ToString();

    [Parameter] public List<BoundingBox> BoundingBoxes { get; set; } = new();
    [Parameter] public EventCallback<List<BoundingBox>> BoundingBoxesChanged { get; set; }

    private DotNetObjectReference<LabelDrawerContainer>? _dotNetRef;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("labelDrawerContainerHelper.SetRefHandler", _dotNetRef);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable("ReceiveBoundingBoxes")]
    public void ReceiveBoundingBoxes(List<BoundingBox> boundingBoxes)
    {
        BoundingBoxes = [..boundingBoxes];
        BoundingBoxesChanged.InvokeAsync(boundingBoxes);
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
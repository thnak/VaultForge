using BusinessModels.General;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebApp.Client.Components;

public partial class LabelDrawerContainer : ComponentBase, IDisposable
{
    private readonly string _containerId = Guid.NewGuid().ToString();

    private List<BoundingBox> BoundingBoxes { get; set; } = new();

    private DotNetObjectReference<LabelDrawerContainer>? _dotNetRef;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("labelDrawerContainerHelper.SetRefHandler", _dotNetRef, "LabelDrawerContainer", "https://cdn.pixabay.com/photo/2024/09/20/01/37/dubai-creek-9060098_640.jpg");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable("ReceiveBoundingBoxes")]
    public void ReceiveBoundingBoxes(List<BoundingBox> boundingBoxes)
    {
        BoundingBoxes = [..boundingBoxes];
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
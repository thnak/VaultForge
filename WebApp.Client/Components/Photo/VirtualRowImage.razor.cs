using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Components.Photo;

public partial class VirtualRowImage(ILogger<VirtualRowImage> logger) : ComponentBase, IDisposable
{
    [Parameter] public List<VirtualImage> Images { get; set; } = new();
    [Category("Common")] [Parameter] public string Class { get; set; } = string.Empty;
    [Parameter] public int WindowsHeight { get; set; } = 300;

    private readonly Guid _guidKey = Guid.NewGuid();

    #region -- models --

    public class VirtualImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Src { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
    }

    #endregion

    private RenderFragment? _imageRenderer;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            BrowserViewportService.SubscribeAsync(_guidKey, BrowserChange);
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await PrepareGrid();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task BrowserChange(BrowserViewportEventArgs _)
    {
        await PrepareGrid();
        await InvokeAsync(StateHasChanged);
    }

    
    private async Task PrepareGrid()
    {
        var windowSize = await BrowserViewportService.GetCurrentBrowserWindowSizeAsync();
        if(Images.Count == 0) {
            logger.LogInformation("Empty");
            return; }
        
        var packElementsIntoContainers = Images.PackElementsIntoContainers(windowSize.Width, image => image.Width);
        packElementsIntoContainers.Shuffle();
        foreach (var image in packElementsIntoContainers)
        {
            image.Shuffle();
        }

        var imageHeight = WindowsHeight / packElementsIntoContainers.Count;
        
        logger.LogInformation($"{_guidKey} with {packElementsIntoContainers.Sum(x=>x.Count)} images");
        
        _imageRenderer = builder =>
        {
            int index = 1;
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "id", _guidKey);
            builder.AddAttribute(2, "class", Class);
            builder.AddAttribute(3, "style", $"height:{WindowsHeight}px");

            foreach (var image in packElementsIntoContainers.SelectMany(x => x))
            {
                var (height, width) = ImageResizer.ResizeByHeight(image.Height, image.Width, imageHeight);
                builder.OpenElement(index++, "img");
                builder.AddAttribute(0, "src", image.Src);
                builder.AddAttribute(1, "width", width);
                builder.AddAttribute(2, "height", height);
                builder.AddAttribute(3, "alt", image.Alt);
                builder.AddAttribute(4, "id", image.Id);
                builder.AddAttribute(5, "loading", "lazy");
                builder.CloseElement();
            }

            builder.CloseElement();
        };
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        BrowserViewportService.UnsubscribeAsync(_guidKey);
    }
}
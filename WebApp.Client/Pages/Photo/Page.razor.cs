using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using WebApp.Client.Components.Photo;

namespace WebApp.Client.Pages.Photo;

public partial class Page : ComponentBase
{
    private async ValueTask<ItemsProviderResult<List<VirtualRowImage.VirtualImage>>> ItemsProvider(ItemsProviderRequest request)
    {
        await Task.Delay(500);
        List<List<VirtualRowImage.VirtualImage>> im = [];
        Random random = new();

        for (int i = 0; i < request.Count; i++)
        {
            List<VirtualRowImage.VirtualImage> items = new(request.Count);

            foreach (var item in items)
            {
                item.Height = random.Next(500, 4096);
                item.Width = random.Next(500, 4096);
                item.Src = "api/files/get-file?id=674a79b32d77d10d63730c10&type=ThumbnailWebpFile";
            }

            im.Add(items);
        }


        return new ItemsProviderResult<List<VirtualRowImage.VirtualImage>>(im, 1_000);
    }
}
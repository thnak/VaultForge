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

        for (int i = 0; i < 100; i++)
        {
            List<VirtualRowImage.VirtualImage> items = new();

            for (int j = 0; j < 10; j++)
            {
                var item = new VirtualRowImage.VirtualImage();
                item.Height = random.Next(500, 4096);
                item.Width = random.Next(500, 4096);
                item.Src = "api/files/get-file?id=674a79b32d77d10d63730c10&type=ThumbnailWebpFile";
                items.Add(item);
            }

            im.Add(items);
        }


        return new ItemsProviderResult<List<VirtualRowImage.VirtualImage>>(im, 100);
    }
}
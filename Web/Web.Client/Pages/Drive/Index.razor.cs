using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Web.Client.Pages.Drive;

public partial class Index : ComponentBase
{
    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }
    
    private List<DropItem> _items = new()
    {
        new DropItem(){ Name = "Untitled document", Identifier = "Files" },
        new DropItem(){ Name = "GoonSwarmBestSwarm.png", Identifier = "Files" },
        new DropItem(){ Name = "co2traitors.txt", Identifier = "Files" },
        new DropItem(){ Name = "import.csv", Identifier = "Files" },
        new DropItem(){ Name = "planned_components_2022-2023.txt", Identifier = "Files" },
    };
    
    public class DropItem
    {
        public string Name { get; init; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
    }
}
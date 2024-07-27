using System.Net.Mime;
using System.Text;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Services.Http;

namespace WebApp.Client.Pages.Drive.SharedDrive;

public partial class Page(BaseHttpClientService baseClientService) : ComponentBase, IDisposable
{
    private bool Open { get; set; }
    private bool Loading { get; set; }

    private FolderInfoModel RootFolder { get; set; } = new();

    private MudDropContainer<DropItem>? DropContainer { get; set; }

    private List<DropItem> Items { get; } =
    [
        new DropItem { Name = "Untitled document", Identifier = "File" },
        new DropItem { Name = "GoonSwarmBestSwarm.png", Identifier = "File" },
        new DropItem { Name = "co2traitors.txt", Identifier = "File" },
        new DropItem { Name = "import.csv", Identifier = "File" },
        new DropItem { Name = "planned_components_2022-2023.txt", Identifier = "File" }
    ];

    public void Dispose()
    {
        EventListener.ContextMenuClickedWithParamAsync -= ContextMenuClick;
    }

    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        if (dropItem.Item != null) dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }

    #region Init

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetRootFolderAsync().ConfigureAwait(false);
            EventListener.ContextMenuClickedWithParamAsync += ContextMenuClick;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region Event

    private Task ContextMenuClick(int x, int y)
    {
        Console.WriteLine($"ClientX: {x}\nClientY: {y}");
        return Task.CompletedTask;
    }

    #endregion

    private void OpenAddPopUp()
    {
        Open = true;
    }

    #region Models

    public class DropItem
    {
        public string Name { get; init; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }

    #endregion

    #region Get Data

    private async Task GetRootFolderAsync()
    {
        Loading = true;
        await Task.Delay(1);
        await InvokeAsync(StateHasChanged);
        Items.Clear();
        var responseMessage = await baseClientService.HttpClient.GetAsync("/api/Files/get-shared-folder");
        if (responseMessage.IsSuccessStatusCode)
        {
            var textPlant = await responseMessage.Content.ReadAsStringAsync();
            var folder = textPlant.DeSerialize<FolderInfoModel>();
            if (folder != null)
            {
                RootFolder = folder;
                var fileCodes = RootFolder.Contents.Where(x => x is { Type: FolderContentType.File or FolderContentType.DeletedFile or FolderContentType.HiddenFile }).Select(x => x.Id).ToList();
                var folderCodes = RootFolder.Contents.Where(x => x is { Type: FolderContentType.Folder or FolderContentType.DeletedFolder or FolderContentType.HiddenFolder }).Select(x => x.Id).ToList();
                var folders = await GetFolders([..folderCodes]);
                var files = await GetFiles([..fileCodes]);

                foreach (var file in files)
                    Items.Add(new DropItem
                    {
                        Identifier = "File",
                        ContentType = file.ContentType,
                        Name = file.FileName
                    });

                foreach (var file in folders)
                    Items.Add(new DropItem
                    {
                        Identifier = "Folder",
                        Name = file.FolderName
                    });
            }
        }

        Loading = false;
        DropContainer?.Refresh();
        await Task.Delay(1);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<List<FileInfoModel>> GetFiles(List<string> codes)
    {
        var textPlant = new StringContent(codes.ToJson(), Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await baseClientService.HttpClient.PostAsync("/api/Files/get-file-list", textPlant);
        if (response.IsSuccessStatusCode)
        {
            var textPlan = await response.Content.ReadAsStringAsync();
            var listFiles = textPlan.DeSerialize<List<FileInfoModel>>();
            if (listFiles != null) return listFiles;
        }

        return [];
    }

    private async Task<List<FolderInfoModel>> GetFolders(string[] codes)
    {
        var textPlant = new StringContent(codes.ToJson(), Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await baseClientService.HttpClient.PostAsync("/api/Files/get-folder-list", textPlant);
        if (response.IsSuccessStatusCode)
        {
            var textPlan = await response.Content.ReadAsStringAsync();
            var listFiles = textPlan.DeSerialize<List<FolderInfoModel>>();
            if (listFiles != null) return listFiles;
        }

        return [];
    }

    #endregion
}
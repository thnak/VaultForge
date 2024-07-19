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

    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        if (dropItem.Item != null) dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }

    private List<DropItem> Items { get; set; } =
    [
        new DropItem() { Name = "Untitled document", Identifier = "Files" },
        new DropItem() { Name = "GoonSwarmBestSwarm.png", Identifier = "Files" },
        new DropItem() { Name = "co2traitors.txt", Identifier = "Files" },
        new DropItem() { Name = "import.csv", Identifier = "Files" },
        new DropItem() { Name = "planned_components_2022-2023.txt", Identifier = "Files" }
    ];

    #region Models

    public class DropItem
    {
        public string Name { get; init; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
    }

    #endregion

    #region Init

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetRootFolderAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
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
                var fileCodes = RootFolder.Contents.Where(x => x.Type == FolderContentType.File).Select(x => x.Id).ToList();
                var folderCodes = RootFolder.Contents.Where(x => x.Type == FolderContentType.Folder).Select(x => x.Id).ToList();
                var folders = await GetFolders([..folderCodes]);
                var files = await GetFiles([..fileCodes]);

                foreach (var file in files)
                {
                    Items.Add(new()
                    {
                        Identifier = "Files",
                        Name = file.FileName
                    });
                }

                foreach (var file in folders)
                {
                    Items.Add(new()
                    {
                        Identifier = "Folders",
                        Name = file.FolderName
                    });
                }
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

    public void Dispose()
    {
        //
    }
}
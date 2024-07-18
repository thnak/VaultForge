using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Services.Http;

namespace WebApp.Client.Pages.Drive.SharedDrive;

public partial class Page(BaseHttpClientService baseClientService) : ComponentBase
{
    private bool Open { get; set; }
    private bool Loading { get; set; }

    private FolderInfoModel RootFolder { get; set; } = new();

    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        if (dropItem.Item != null) dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }

    private List<DropItem> Items { get; set; } = [];

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
    }

    private async Task<List<FileInfoModel>> GetFiles(List<string> codes)
    {
        MultipartFormDataContent formDataContent = new MultipartFormDataContent();
        formDataContent.Add(new StringContent("listFiles"), codes.ToJson());
        var response = await baseClientService.HttpClient.PostAsync("/api/Files/get-file-list", formDataContent);
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
        MultipartFormDataContent formDataContent = new MultipartFormDataContent();
        formDataContent.Add(new StringContent("listFolders"), codes.ToJson());
        var response = await baseClientService.HttpClient.PostAsync("/api/Files/get-folder-list", formDataContent);
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
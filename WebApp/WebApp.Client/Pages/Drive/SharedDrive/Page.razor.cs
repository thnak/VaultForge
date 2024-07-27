using System.Net.Mime;
using System.Text;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Services.Http;

namespace WebApp.Client.Pages.Drive.SharedDrive;

public partial class Page(BaseHttpClientService baseClientService) : ComponentBase, IDisposable
{
    private bool Open { get; set; }
    private bool Loading { get; set; }

    private FolderInfoModel RootFolder { get; set; } = new();

    private MudDropContainer<DropItem>? DropContainer { get; set; }

    private List<DropItem> Items { get; } = [];

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
        public Action Download { get; set; } = () => { };
        public Action Rename { get; set; } = () => { };
        public Action Share { get; set; } = () => { };
        public Action GetLink { get; set; } = () => { };
        public Action MoveTo { get; set; } = () => { };
        public Action Delete { get; set; } = () => { };
        public Action GetInformation { get; set; } = () => { };
    }

    #endregion

    #region Get Data

    private async Task GetRootFolderAsync()
    {
        Loading = true;
        await Task.Delay(1);
        await InvokeAsync(StateHasChanged);
        Items.Clear();
        var responseMessage = await baseClientService.GetAsync<FolderInfoModel>("/api/Files/get-shared-folder");
        if (responseMessage.IsSuccessStatusCode)
        {
            var folder = responseMessage.Data;
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
                        Name = file.FileName,
                        Rename = () => RenameFile(file.Id.ToString(), file.FileName).ConfigureAwait(false)
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
        var response = await baseClientService.PostAsync<List<FileInfoModel>>("/api/Files/get-file-list", textPlant);
        if (response.IsSuccessStatusCode)
        {
            return response.Data ?? [];
        }

        ToastService.ShowError("Empty files");

        return [];
    }

    private async Task<List<FolderInfoModel>> GetFolders(string[] codes)
    {
        var textPlant = new StringContent(codes.ToJson(), Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await baseClientService.PostAsync<List<FolderInfoModel>>("/api/Files/get-folder-list", textPlant);
        if (response.IsSuccessStatusCode)
        {
            return response.Data ?? [];
        }

        ToastService.ShowError("Empty foldes");
        return [];
    }

    #endregion

    #region Event handler

    private async Task RenameFile(string id, string oldName)
    {
        Loading = true;
        await InvokeAsync(StateHasChanged);
        var option = new DialogOptions()
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialogParam = new DialogParameters<ConfirmWithFieldDialog>()
        {
            { x => x.FieldName, AppLang.FileName },
            { x => x.OldValueField, oldName }
        };
        var dialog = await DialogService.ShowAsync<ConfirmWithFieldDialog>(AppLang.ReName, dialogParam, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: string newName })
        {
            MultipartFormDataContent formDataContent = new MultipartFormDataContent();
            formDataContent.Add(new StringContent(newName), "newName");
            formDataContent.Add(new StringContent(id), "objectId");

            var response = await ApiService.PostAsync<string>("/api/files/re-name-file", formDataContent);
            if (response.IsSuccessStatusCode)
            {
                ToastService.ShowSuccess(response.Message);
                await GetRootFolderAsync().ConfigureAwait(false);
            }
            else
            {
                ToastService.ShowError(response.Message);
            }
        }

        Loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveFile(string id)
    {
    }

    #endregion
}
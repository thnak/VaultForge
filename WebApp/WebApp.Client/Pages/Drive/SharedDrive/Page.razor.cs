using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using Blazored.Toast.Configuration;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MongoDB.Bson;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;
using WebApp.Client.Services.Http;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.Drive.SharedDrive;

[SupportedOSPlatform("browser")]
public partial class Page(BaseHttpClientService baseClientService) : ComponentBase, IDisposable
{
    #region Js Module

    [JSImport("getMessage", nameof(Page))]
    internal static partial string GetWelcomeMessageJs();

    [JSImport("uploadFile", nameof(Page))]
    internal static partial void UpLoadFilesJs([StringSyntax(StringSyntaxAttribute.Uri)] string api, string folder);

    [JSImport("getSelectedFiles", nameof(Page))]
    internal static partial string[] GetSelectedFilesJs();

    [JSImport("clearSelectedFiles", nameof(Page))]
    internal static partial void ClearSelectedFileJs();


    /// <summary>
    /// 
    /// </summary>
    /// <param name="values">total, current</param>
    [JSInvokable("UpFileLoadProgressJsInvoke")]
    public static void UpFileLoadProgressJsInvoke(double[] values)
    {
        ProgressEvent?.Invoke(values);
    }

    [JSInvokable("OnComplete")]
    public static void OnUpLoadCompleteJs(int status, string response)
    {
        OnCompleteEvent?.Invoke(status, response);
    }

    [JSInvokable("OnError")]
    public static void OnUpLoadErrorJs(int status, string response)
    {
        OnErrorEvent?.Invoke(status, response);
    }

    private static Func<int, string, Task>? OnCompleteEvent { get; set; }
    private static Func<int, string, Task>? OnErrorEvent { get; set; }
    private static Func<double[], Task>? ProgressEvent { get; set; }

    #endregion

    [Parameter] public string? FolderId { get; set; } = string.Empty;
    private readonly CancellationTokenSource _cts = new();

    private bool Open { get; set; }
    private bool Loading { get; set; }
    private bool ShouldRen { get; set; } = true;
    private FolderInfoModel RootFolder { get; set; } = new();
    private MudDropContainer<DropItem>? DropContainer { get; set; }
    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = [];

    #region Upload properties

    private List<FileInfoModel> FileUploadList { get; set; } = [];
    private Dictionary<ObjectId, double> UploadProgress { get; set; } = [];

    #endregion

    private List<DropItem> FileItemList { get; } = [];
    private List<DropItem> FolderItemList { get; } = [];
    private bool Uploading { get; set; }

    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        if (dropItem.Item != null) dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }

    #region Event

    private Task ContextMenuClick(int x, int y)
    {
        Console.WriteLine(AppLang.Page_ContextMenuClick_, x, y);
        return Task.CompletedTask;
    }

    private Task FileInputChanged()
    {
        var list = GetSelectedFilesJs();
        return LoadedFileList(list);
    }

    private Task ClearSelectedFile()
    {
        ClearSelectedFileJs();
        return FileInputChanged();
    }

    private Task SendForm()
    {
        Uploading = true;
        UpLoadFilesJs(ApiService.GetBaseUrl() + "api/Files/upload-physical", RootFolder.Id.ToString());
        return Task.CompletedTask;
    }

    private void OpenAddPopUp()
    {
        Open = true;
    }

    private Task LoadedFileList(IEnumerable<string> arg)
    {
        FileUploadList.Clear();
        UploadProgress.Clear();
        foreach (var fileName in arg)
        {
            FileInfoModel model = new FileInfoModel() { FileName = fileName };
            FileUploadList.Add(model);
            UploadProgress.Add(model.Id, 0);
        }

        return InvokeAsync(StateHasChanged);
    }

    private Task UpdateProgressBar(double[] arg)
    {
        var percent = arg[1] / arg[0] * 100;
        foreach (var pa in UploadProgress.Keys)
        {
            UploadProgress[pa] = percent;
        }
        return InvokeAsync(StateHasChanged);
    }

    private Task OnError(int arg1, string arg2)
    {
        Uploading = false;
        ToastService.ShowError(arg2, ToastSettings);
        return InvokeAsync(StateHasChanged);
    }

    private Task OnComplete(int arg1, string arg2)
    {
        Uploading = false;
        foreach (var pa in UploadProgress.Keys)
        {
            UploadProgress[pa] = 100;
        }
        ToastService.ShowSuccess(arg2, ToastSettings);
        return InvokeAsync(StateHasChanged);
    }

    private Task KeyPressChangeEventAsync(string arg)
    {
        if (arg == KeyBoardNames.Enter)
            return GetRootFolderAsync();
        return InvokeAsync(StateHasChanged);
    }

    #endregion



    void ToastSettings(ToastSettings toastSettings)
    {
        toastSettings.AdditionalClasses = "toast-move-right-2-left";
    }

    #region Models

    public class DropItem
    {
        public string Name { get; init; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ItemClassList { get; set; } = string.Empty;
        public ButtonAction Download { get; set; } = new();
        public ButtonAction Rename { get; set; } = new();
        public ButtonAction Share { get; set; } = new();
        public ButtonAction GetLink { get; set; } = new();
        public ButtonAction MoveTo { get; set; } = new();
        public ButtonAction Delete { get; set; } = new();
        public ButtonAction GetInformation { get; set; } = new();
        public ButtonAction DbLickEvent { get; set; } = new();
    }

    #endregion

    #region Init

    protected override bool ShouldRender()
    {
        return ShouldRen;
    }

    public void Dispose()
    {
        EventListener.ContextMenuClickedWithParamAsync -= ContextMenuClick;
        EventListener.KeyPressChangeEventAsync -= KeyPressChangeEventAsync;
        ProgressEvent -= UpdateProgressBar;
        OnErrorEvent -= OnError;
        OnCompleteEvent -= OnComplete;
        _cts.Cancel();
        _cts.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = Task.Run(GetRootFolderAsync);
            await JSHost.ImportAsync(nameof(Page), $"/Pages/Drive/SharedDrive/{nameof(Page)}.razor.js", _cts.Token);
            EventListener.KeyPressChangeEventAsync += KeyPressChangeEventAsync;
            EventListener.ContextMenuClickedWithParamAsync += ContextMenuClick;
            ProgressEvent += UpdateProgressBar;
            OnErrorEvent += OnError;
            OnCompleteEvent += OnComplete;
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private async Task Render()
    {
        ShouldRen = true;
        await InvokeAsync(StateHasChanged);
        ShouldRen = false;
    }

    #endregion

    #region Get Data

    private async Task GetRootFolderAsync()
    {
        Loading = true;
        await Render();
        FileItemList.Clear();
        FolderItemList.Clear();
        var responseMessage = await baseClientService.GetAsync<FolderInfoModel>($"/api/Files/get-folder?id={FolderId}");
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
                    FileItemList.Add(new DropItem
                    {
                        Identifier = "File",
                        ContentType = file.ContentType,
                        Name = file.FileName,
                        Rename = new ButtonAction
                        {
                            Action = () => RenameFile(file.Id.ToString(), file.FileName, "/api/files/re-name-file").ConfigureAwait(false)
                        },
                        Download = new ButtonAction
                        {
                            Action = () => Download(file.Id.ToString()).ConfigureAwait(false)
                        },
                        Delete = new ButtonAction
                        {
                            Action = () => DeleteFile(file).ConfigureAwait(false)
                        },
                        GetLink = new ButtonAction
                        {
                            Action = () => Copy2ClipBoard($"{ApiService.GetBaseUrl()}api/Files/download-file?id={file.Id}")
                        },
                        MoveTo = new ButtonAction()
                        {
                            Action = () => MoveFile2Folder(file).ConfigureAwait(false)
                        },
                        ItemClassList = InitStyleElement(file.Type)
                    });

                foreach (var f in folders)
                    FolderItemList.Add(new DropItem
                    {
                        Identifier = "Folder",
                        Name = f.FolderName,
                        ItemClassList = InitStyleElement(f.Type),
                        DbLickEvent = new ButtonAction
                        {
                            Action = () => OpenFolder(f.Id.ToString())
                        },
                        Rename = new ButtonAction
                        {
                            Action = () => RenameFile(f.Id.ToString(), f.FolderName, "/api/files/re-name-folder").ConfigureAwait(false)
                        },
                        Delete = new ButtonAction()
                        {
                            Action = () => DeleteFolder(f).ConfigureAwait(false)
                        }
                    });
            }
        }


        Loading = false;
        DropContainer?.Refresh();
        await Render();
        ShouldRen = true;
        _ = Task.Run(InitBreadcrumb);
    }

    string InitStyleElement(FolderContentType type)
    {
        return "align-center justify-center d-flex flex-row gap-2 mud-elevation-1 mud-paper mud-paper-outlined pa-2 rounded-lg smooth-appear".ItemOpacityClass(type);
    }

    string InitStyleElement(FileContentType type)
    {
        return "align-center justify-center d-flex flex-row gap-2 mud-elevation-1 mud-paper mud-paper-outlined pa-2 rounded-lg smooth-appear".ItemOpacityClass(type);
    }

    private async Task InitBreadcrumb()
    {
        BreadcrumbItems = [];
        var response = await ApiService.GetAsync<List<FolderInfoModel>>($"/api/files/get-folder-blood-line?id={RootFolder.Id.ToString()}", _cts.Token);
        if (response is { IsSuccessStatusCode: true, Data: not null })
            foreach (var folderInfoModel in response.Data)
                BreadcrumbItems.Add(new BreadcrumbItem(folderInfoModel.FolderName, PageRoutes.Drive.Shared.Src + $"/{folderInfoModel.Id.ToString()}"));

        await Render();
        ShouldRen = true;
    }

    private async Task<List<FileInfoModel>> GetFiles(List<string> codes)
    {
        var textPlant = new StringContent(StringExtension.ToJson(codes), Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await baseClientService.PostAsync<List<FileInfoModel>>("/api/Files/get-file-list", textPlant, _cts.Token);
        if (response.IsSuccessStatusCode) return response.Data ?? [];

        ToastService.ShowError("Empty files", ToastSettings);

        return [];
    }

    private async Task<List<FolderInfoModel>> GetFolders(string[] codes)
    {
        var textPlant = new StringContent(StringExtension.ToJson(codes), Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await baseClientService.PostAsync<List<FolderInfoModel>>("/api/Files/get-folder-list", textPlant, _cts.Token);
        if (response.IsSuccessStatusCode) return response.Data ?? [];

        ToastService.ShowError("Empty folders", ToastSettings);
        return [];
    }

    #endregion

    #region Event handler

    private async Task OpenAddNewFolder(MouseEventArgs obj)
    {
        var option = new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialog = await DialogService.ShowAsync<AddNewFolderDialog>(AppLang.New_folder, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: string name })
        {
            var model = new RequestNewFolderModel
            {
                NewFolder = new FolderInfoModel
                {
                    FolderName = name
                },
                RootId = RootFolder.Id.ToString(),
                RootPassWord = RootFolder.Password
            };
            var content = new StringContent(StringExtension.ToJson(model), Encoding.Unicode, MimeTypeNames.Application.Json);
            var response = await ApiService.PutAsync<string>("api/Files/create-folder", content, _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                ToastService.ShowSuccess(response.Message, ToastSettings);


                _ = Task.Run(GetRootFolderAsync);
            }
            else
            {
                Console.WriteLine(response.Message);
            }
        }
    }

    private async Task RenameFile(string id, string oldName, [StringSyntax("Uri")] string url)
    {
        Loading = true;
        await Render();
        var option = new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialogParam = new DialogParameters<ConfirmWithFieldDialog>
        {
            { x => x.FieldName, AppLang.FileName },
            { x => x.OldValueField, oldName }
        };
        var dialog = await DialogService.ShowAsync<ConfirmWithFieldDialog>(AppLang.ReName, dialogParam, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: string newName })
        {
            if (newName.ValidateSystemPathName(out var keyword))
            {
                var formDataContent = new MultipartFormDataContent();
                formDataContent.Add(new StringContent(newName, Encoding.UTF8, MediaTypeNames.Application.Json), "newName");
                formDataContent.Add(new StringContent(id, Encoding.UTF8, MediaTypeNames.Application.Json), "objectId");

                var response = await ApiService.PostAsync<string>(url, formDataContent, _cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    ToastService.ShowSuccess(response.Message, ToastSettings);
                    _ = Task.Run(GetRootFolderAsync);
                }
                else
                {
                    ToastService.ShowError(response.Message, ToastSettings);
                }
            }
            else
            {
                ToastService.ShowError(string.Format(AppLang.File_name_contains_invalid_character__x, keyword), ToastSettings);
            }
        }

        Loading = false;
        await Render();
        ShouldRen = true;
        await InvokeAsync(StateHasChanged);
    }


    private async Task Download(string id)
    {
        await JsRuntime.Download($"{ApiService.GetBaseUrl()}api/files/download-file?id={id}");
    }

    private void Copy2ClipBoard([StringSyntax(StringSyntaxAttribute.Uri)] string link)
    {
        JsRuntime.CopyToClipBoard(link);
        ToastService.ShowSuccess(AppLang.Copied, ToastSettings);
    }

    private void OpenFolder(string id)
    {
        Navigation.NavigateTo(PageRoutes.Drive.Shared.Src + $"/{id}");
    }

    private async Task DeleteFile(FileInfoModel file)
    {
        var data = new DialogConfirmDataModel
        {
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.SetKey(file);
                builder.AddContent(1, "Chuyển vào thùng rác?");
                builder.CloseElement();
            },
            Title = AppLang.Warning,
            Icon = "fa-solid fa-triangle-exclamation",
            Color = Color.Error
        };
        var option = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var parameter = new DialogParameters<ConfirmDialog>
        {
            { x => x.DataModel, data }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("", parameter, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            var response = await ApiService.DeleteAsync<string>($"/api/Files/safe-delete-file?code={file.Id.ToString()}", _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                await GetRootFolderAsync();
                ToastService.ShowSuccess(AppLang.Delete_successfully, ToastSettings);
            }
            else
            {
                ToastService.ShowError(response.Message, ToastSettings);
            }
        }
    }


    private async Task DeleteFolder(FolderInfoModel folder)
    {
        var data = new DialogConfirmDataModel
        {
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.SetKey(folder.Id);
                builder.AddContent(1, "Chuyển vào thùng rác?");
                builder.CloseElement();
            },
            Title = AppLang.Warning,
            Icon = "fa-solid fa-triangle-exclamation",
            Color = Color.Error
        };
        var option = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var parameter = new DialogParameters<ConfirmDialog>
        {
            { x => x.DataModel, data }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("", parameter, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            var response = await ApiService.DeleteAsync<string>($"/api/Files/safe-delete-folder?code={folder.Id.ToString()}", _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                await GetRootFolderAsync();
                ToastService.ShowSuccess(AppLang.Delete_successfully, ToastSettings);
            }
            else
            {
                ToastService.ShowError(response.Message, ToastSettings);
            }
        }
    }

    private async Task MoveFile2Folder(FileInfoModel file)
    {
        var option = new DialogOptions()
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var param = new DialogParameters<FolderSelectorDialog>()
        {
            { x => x.Folder, RootFolder },
        };
        var dialog = await DialogService.ShowAsync<FolderSelectorDialog>($"{AppLang.Move__0_.AutoReplace(file.FileName)}", param, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: true })
        {
            await GetRootFolderAsync();
        }
    }

    #endregion
}
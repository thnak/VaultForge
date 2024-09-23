using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using BusinessModels.WebContent.Drive;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MongoDB.Bson;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Components.Menus;
using WebApp.Client.Models;
using WebApp.Client.Pages.Drive.View.Dialog;
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

    [JSImport("getSelectedFileSize", nameof(Page))]
    internal static partial double[] GetSelectedFileSizeJs();

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

    #region Page params

    [SupplyParameterFromQuery(Name = "FolderId")]
    public string? FolderId { get; set; }

    [SupplyParameterFromQuery(Name = "status")]
    public string? PageStatus { get; set; }

    [SupplyParameterFromQuery(Name = "page")]
    public int? PageIndex { get; set; } = 1;

    [SupplyParameterFromQuery(Name = "size")]
    public int? QuerySize { get; set; } = 50;

    #endregion


    private readonly CancellationTokenSource _cts = new();

    private bool Open { get; set; }
    private bool Loading { get; set; }

    private string Password { get; set; } = string.Empty;

    #region Pagination fields

    private int TotalPages { get; set; }

    #endregion

    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 50;

    private bool CanBeDrag { get; set; }

    private bool ShouldRen { get; set; } = true;
    private FolderInfoModel RootFolder { get; set; } = new();
    private MudDropContainer<DropItem>? DropContainer { get; set; }
    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = [];

    private Dictionary<LayoutSelectType, bool> FolderLayoutSelects { get; set; } = new();
    private Dictionary<LayoutSelectType, bool> FileLayoutSelects { get; set; } = new();

    #region Upload fields

    private List<FileInfoModel> FileUploadList { get; set; } = [];
    private Dictionary<ObjectId, double> UploadProgress { get; set; } = [];
    private bool Uploading { get; set; }

    #endregion

    private List<DropItem> FileItemList { get; } = [];
    private List<DropItem> FolderItemList { get; } = [];

    /// <summary>
    /// Hiển thị các tài nguyên ẩn
    /// </summary>
    private bool ShowHidden { get; set; }

    #region Events

    #region Drop events

    private void ItemUpdated(MudItemDropInfo<DropItem> dropItem)
    {
        if (dropItem.Item != null) dropItem.Item.Identifier = dropItem.DropzoneIdentifier;
    }

    #endregion

    #region Clicks

    private Task ContextMenuClick(int x, int y)
    {
        Console.WriteLine(AppLang.Page_ContextMenuClick_, x, y);
        return Task.CompletedTask;
    }

    private Task KeyPressChangeEventAsync(string arg)
    {
        if (arg == KeyBoardNames.Enter)
            return GetRootFolderAsync();
        return InvokeAsync(StateHasChanged);
    }

    #endregion

    #region upload events

    private Task FileInputChanged()
    {
        return LoadedFileList(GetSelectedFilesJs(), GetSelectedFileSizeJs());
    }

    private Task ClearSelectedFile()
    {
        ClearSelectedFileJs();
        return FileInputChanged();
    }

    private Task SendForm()
    {
        Uploading = true;
        UpLoadFilesJs(baseClientService.GetBaseUrl() + "api/Files/upload-physical", RootFolder.Id.ToString());
        return Task.CompletedTask;
    }

    private void OpenAddPopUp()
    {
        Open = true;
    }

    private Task LoadedFileList(string[] arg, double[] size)
    {
        FileUploadList.Clear();
        UploadProgress.Clear();
        for (int i = 0; i < arg.Length; i++)
        {
            FileInfoModel model = new FileInfoModel() { FileName = arg[i], FileSize = (long)size[i] };
            FileUploadList.Add(model);
            UploadProgress.Add(model.Id, 0);
        }

        return InvokeAsync(StateHasChanged);
    }

    private Task UpdateProgressBar(double[] arg)
    {
        int index = 0;
        foreach (var pa in UploadProgress.Keys)
        {
            if (index == 0)
            {
                var current = Math.Min(arg[1], FileUploadList[index].FileSize);
                UploadProgress[pa] = current;
            }
            else
            {
                var current = Math.Min(Math.Max(arg[1] - FileUploadList[..index].Sum(x => x.FileSize), 0), FileUploadList[index].FileSize);
                UploadProgress[pa] = current;
            }

            index++;
        }

        return InvokeAsync(StateHasChanged);
    }

    private Task OnError(int arg1, string arg2)
    {
        Uploading = false;
        ToastService.ShowError(arg2, TypeClassList.ToastDefaultSetting);
        return InvokeAsync(StateHasChanged);
    }

    private Task OnComplete(int arg1, string arg2)
    {
        Uploading = false;
        Open = false;
        int index = 0;
        foreach (var pa in UploadProgress.Keys)
        {
            UploadProgress[pa] = FileUploadList[index++].FileSize;
        }

        ToastService.ShowSuccess(arg2, TypeClassList.ToastDefaultSetting);
        return GetRootFolderAsync(Password);
    }

    #endregion

    #endregion


    #region Models

    public class DropItem
    {
        public string Name { get; init; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ItemClassList { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-solid fa-folder";
        public string Thumbnail { get; set; } = string.Empty;
        public RenderFragment? Menu { get; set; }
        public ButtonAction Open { get; set; } = new();
        public ButtonAction Download { get; set; } = new();
        public ButtonAction Rename { get; set; } = new();
        public ButtonAction Share { get; set; } = new();
        public ButtonAction GetLink { get; set; } = new();
        public ButtonAction MoveTo { get; set; } = new();
        public ButtonAction Delete { get; set; } = new();
        public ButtonAction GetInformation { get; set; } = new();
        public ButtonAction DbLickEvent { get; set; } = new();
    }

    private enum LayoutSelectType
    {
        List,
        Grid,
        Title
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

    protected override void OnInitialized()
    {
        FileLayoutSelects = new() { { LayoutSelectType.List, false }, { LayoutSelectType.Grid, false }, { LayoutSelectType.Title, true } };
        FolderLayoutSelects = new() { { LayoutSelectType.List, false }, { LayoutSelectType.Grid, false }, { LayoutSelectType.Title, true } };
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        PageSize = QuerySize ?? PageSize;
        CurrentPage = PageIndex ?? CurrentPage;
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitLayout();
            _ = Task.Run(() => GetRootFolderAsync());
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

    private async Task InitLayout()
    {
        var folderLayout = await JsRuntime.GetLocalStorage<Dictionary<LayoutSelectType, bool>>(nameof(FolderLayoutSelects));
        var fileLayout = await JsRuntime.GetLocalStorage<Dictionary<LayoutSelectType, bool>>(nameof(FileLayoutSelects));
        if (folderLayout != default)
        {
            FolderLayoutSelects = folderLayout;
        }
        else
        {
            await JsRuntime.SetLocalStorage(nameof(FolderLayoutSelects), FolderLayoutSelects);
        }

        if (fileLayout != default)
        {
            FileLayoutSelects = fileLayout;
        }
        else
        {
            await JsRuntime.SetLocalStorage(nameof(FileLayoutSelects), FileLayoutSelects);
        }
    }

    #endregion

    #region Get Data

    private async Task GetRootFolderAsync(string? password = null, bool forceReload = false)
    {
        Loading = true;
        CanBeDrag = EventListener.IsTouchEnabled;
        await Render();

        var formData = new MultipartFormDataContent();
        if (FolderId != null)
            formData.Add(new StringContent(FolderId), "id");
        formData.Add(new StringContent(CurrentPage.ToString()), "page");
        formData.Add(new StringContent(PageSize.ToString()), "pageSize");

        if (password != null)
            formData.Add(new StringContent(password), "password");

        if (PageStatus == "deleted")
        {
            ShowHidden = true;
            FolderContentType[] types = [FolderContentType.DeletedFile, FolderContentType.DeletedFolder];
            formData.Add(new StringContent(types.ToJson()), "contentTypes");
        }

        formData.Add(new StringContent(forceReload.ToJson()), "forceReLoad");

        var responseMessage = await baseClientService.PostAsync<FolderRequest>("/api/Files/get-folder", formData);
        if (responseMessage.IsSuccessStatusCode)
        {
            var folder = responseMessage.Data;
            if (folder != null)
            {
                RootFolder = folder.Folder;
                InitBreadcrumb(folder);
                TotalPages = Math.Max(folder.TotalFolderPages, folder.TotalFilePages);
                FileItemList.Clear();
                foreach (var file in folder.Files)
                    FileItemList.Add(new DropItem
                    {
                        Identifier = "File",
                        ContentType = file.ContentType,
                        Name = file.FileName,
                        Rename = new ButtonAction
                        {
                            Action = () => RenameFile(file.Id.ToString(), file.FileName, "/api/files/re-name-file").ConfigureAwait(false)
                        },
                        Open = new ButtonAction()
                        {
                            Action = () => OpenFileDetailDialog(file.Id.ToString()).ConfigureAwait(false)
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
                            Action = () => Copy2ClipBoard($"{baseClientService.GetBaseUrl()}api/Files/get-file?id={file.Id}")
                        },
                        MoveTo = new ButtonAction()
                        {
                            Action = () => MoveFile2Folder(file).ConfigureAwait(false)
                        },
                        ItemClassList = InitStyleElement(file.Type),
                        Thumbnail = string.IsNullOrEmpty(file.Thumbnail) ? "" : $"{baseClientService.GetBaseUrl()}api/Files/get-file?id={file.Thumbnail}",
                        Menu = InitMenuItem(file)
                    });

                FolderItemList.Clear();
                foreach (var f in folder.Folders)
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
        else if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
        {
            var dialogOption = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };
            var dialog =
                await DialogService.ShowAsync<PasswordRequiredDialog>(AppLang.This_resource_is_protected_by_password,
                    options: dialogOption);
            var dialogResult = await dialog.Result;
            if (dialogResult is { Canceled: false, Data: string pass })
            {
                Password = pass;
                await GetRootFolderAsync(pass);
                return;
            }
        }


        Loading = false;
        DropContainer?.Refresh();
        await Render();
        ShouldRen = true;
    }

    #region Element style

    private string ElementStyle => "mud-height-full align-center justify-center d-flex flex-row gap-2 mud-elevation-1 mud-paper mud-paper-outlined pa-2 rounded-lg smooth-appear";

    string InitStyleElement(FolderContentType type)
    {
        return ElementStyle.ItemOpacityClass(type, ShowHidden);
    }

    string InitStyleElement(FileContentType type)
    {
        return ElementStyle.ItemOpacityClass(type, ShowHidden);
    }

    #endregion


    private void InitBreadcrumb(FolderRequest folderRequest)
    {
        BreadcrumbItems.Clear();
        foreach (var folderInfoModel in folderRequest.BloodLines)
        {
            BreadcrumbItems.Add(new BreadcrumbItem(folderInfoModel.FolderName,
                Navigation.GetUriWithQueryParameters(Navigation.Uri, new Dictionary<string, object?> { { "FolderId", folderInfoModel.Id.ToString() } }),
                false,
                folderInfoModel.Icon == "" ? null : folderInfoModel.Icon));
        }
    }

    private async Task<List<FileInfoModel>> GetFiles(List<string> codes)
    {
        var textPlant = new StringContent(StringExtension.ToJson(codes), Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await baseClientService.PostAsync<List<FileInfoModel>>("/api/Files/get-file-list", textPlant, _cts.Token);
        if (response.IsSuccessStatusCode) return response.Data ?? [];

        ToastService.ShowError("Empty files", TypeClassList.ToastDefaultSetting);

        return [];
    }

    private Task PageChanged(int obj)
    {
        CurrentPage = obj;
        return GetRootFolderAsync(Password);
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
            var content = new StringContent(StringExtension.ToJson(model), Encoding.Unicode,
                MimeTypeNames.Application.Json);
            var response = await baseClientService.PutAsync<string>("api/Files/create-folder", content, _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                ToastService.ShowSuccess(response.Message, TypeClassList.ToastDefaultSetting);
                _ = Task.Run(() => GetRootFolderAsync(forceReload:true));
            }
            else
            {
                ToastService.ShowError(response.Message, TypeClassList.ToastDefaultSetting);
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
                formDataContent.Add(new StringContent(newName, Encoding.UTF8, MediaTypeNames.Application.Json),
                    "newName");
                formDataContent.Add(new StringContent(id, Encoding.UTF8, MediaTypeNames.Application.Json), "objectId");

                var response = await baseClientService.PostAsync<string>(url, formDataContent, _cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    ToastService.ShowSuccess(response.Message, TypeClassList.ToastDefaultSetting);
                    _ = Task.Run(() => GetRootFolderAsync(forceReload:true));
                }
                else
                {
                    ToastService.ShowError(response.Message, TypeClassList.ToastDefaultSetting);
                }
            }
            else
            {
                ToastService.ShowError(string.Format(AppLang.File_name_contains_invalid_character__x, keyword),
                    TypeClassList.ToastDefaultSetting);
            }
        }

        Loading = false;
        await Render();
        ShouldRen = true;
        await InvokeAsync(StateHasChanged);
    }

    private RenderFragment InitMenuItem(FileInfoModel item)
    {
        List<RedditMobileMenu.RedditMobileMenuData> param = new List<RedditMobileMenu.RedditMobileMenuData>();
        param.Add(new RedditMobileMenu.RedditMobileMenuData()
        {
            Title = AppLang.Open,
            Disabled = false,
            Icon = "fa-ellipsis-vertical fa-solid",
            OnClick = () => OpenFileDetailDialog(item.Id.ToString()).ConfigureAwait(false)
        });
        param.Add(new RedditMobileMenu.RedditMobileMenuData()
        {
            Title = AppLang.Download,
            Icon = "cloud-arrow-download",
            OnClick = () => Download(item.Id.ToString()).ConfigureAwait(false)
        });
        param.Add(new RedditMobileMenu.RedditMobileMenuData()
        {
            Title = AppLang.ReName,
            Icon = "fa-solid fa-user-pen",
            OnClick = () => RenameFile(item.Id.ToString(), item.FileName, "/api/files/re-name-file").ConfigureAwait(false)
        });

        return builder =>
        {
            builder.OpenComponent<RedditMobileMenu>(0);
            builder.AddAttribute(0, "Items", param);
            builder.CloseComponent();
        };
    }

    private async Task OpenFileDetailDialog(string id)
    {
        var files = await GetFiles([id]);
        var file = files.FirstOrDefault();
        if (file == null)
        {
            ToastService.ShowError(AppLang.File_could_not_be_found);
            return;
        }

        var option = new DialogOptions()
        {
            FullScreen = true
        };

        var param = new DialogParameters<ImageShowDialog>()
        {
            { x => x.File, file }
        };
        var dialog = await DialogService.ShowAsync<ImageShowDialog>("", param, option);
        await dialog.Result;
    }

    private async Task Download(string id)
    {
        await JsRuntime.Download($"{baseClientService.GetBaseUrl()}api/files/download-file?id={id}");
    }

    private void Copy2ClipBoard([StringSyntax(StringSyntaxAttribute.Uri)] string link)
    {
        JsRuntime.CopyToClipBoard(link);
        ToastService.ShowSuccess(AppLang.Copied, TypeClassList.ToastDefaultSetting);
    }

    private void OpenFolder(string id)
    {
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(PageRoutes.Drive.Shared.Src,
            new Dictionary<string, object?> { { "FolderId", id } }));
    }

    private async Task DeleteFile(FileInfoModel file)
    {
        var data = new DialogConfirmDataModel
        {
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.SetKey(file);
                builder.AddContent(1, file.Type == FileContentType.DeletedFile ? AppLang.Delete_forever : AppLang.Move_to_recycle_bin);
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
            var response = await baseClientService.DeleteAsync<string>($"/api/Files/safe-delete-file?code={file.Id.ToString()}", _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                await GetRootFolderAsync(forceReload: true);
                ToastService.ShowSuccess(AppLang.Delete_successfully, TypeClassList.ToastDefaultSetting);
            }
            else
            {
                ToastService.ShowError(response.Message, TypeClassList.ToastDefaultSetting);
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
                builder.AddContent(1, folder.Type == FolderContentType.DeletedFolder ? AppLang.Delete_forever : AppLang.Move_to_recycle_bin);
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
            var response = await baseClientService.DeleteAsync<string>($"/api/Files/safe-delete-folder?code={folder.Id.ToString()}", _cts.Token);
            if (response.IsSuccessStatusCode)
            {
                await GetRootFolderAsync(forceReload: true);
                ToastService.ShowSuccess(AppLang.Delete_successfully, TypeClassList.ToastDefaultSetting);
            }
            else
            {
                ToastService.ShowError(response.Message, TypeClassList.ToastDefaultSetting);
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
        var dialog =
            await DialogService.ShowAsync<FolderSelectorDialog>($"{AppLang.Move__0_.AutoReplace(file.FileName)}", param,
                option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: true })
        {
            await GetRootFolderAsync();
        }
    }

    #endregion

    #region Layout Event

    private Color GetFolderLayoutColor(LayoutSelectType list)
    {
        return FolderLayoutSelects[list] ? Color.Primary : Color.Default;
    }

    private Color GetFileLayoutColor(LayoutSelectType list)
    {
        return FileLayoutSelects[list] ? Color.Primary : Color.Default;
    }

    #endregion

    private Task FolderLayoutChange(LayoutSelectType list)
    {
        foreach (var pair in FolderLayoutSelects)
        {
            FolderLayoutSelects[pair.Key] = false;
        }

        FolderLayoutSelects[list] = true;
        DropContainer?.Refresh();
        return InvokeAsync(StateHasChanged);
    }

    private Task FileLayoutChange(LayoutSelectType list)
    {
        foreach (var pair in FolderLayoutSelects)
        {
            FileLayoutSelects[pair.Key] = false;
        }

        FileLayoutSelects[list] = true;
        DropContainer?.Refresh();
        return InvokeAsync(StateHasChanged);
    }
}
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace WebApp.Client.Pages.Drive;

public partial class FolderSelectorDialog : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public required FolderInfoModel Folder { get; set; }

    private MudDataGrid<FolderInfoModel> DataGrid { get; set; } = default!;

    private bool Loading { get; set; } = true;

    private FolderInfoModel? SearchString { get; set; }

    private FolderInfoModel CurrentFolder { get; set; } = new();

    protected override void OnParametersSet()
    {
        CurrentFolder = Folder.Copy();
        base.OnParametersSet();
    }


    private void Cancel()
    {
        DialogInstance.Cancel();
    }

    private void Submit()
    {
        DialogInstance.Close(DialogResult.Ok(true));
    }

    private async Task<IEnumerable<FolderInfoModel>> SearchFolder(string arg1, CancellationToken arg2)
    {
        MultipartFormDataContent formDataContent = new();
        formDataContent.Add(new StringContent(arg1), "searchString");
        var response = await ApiService.PostAsync<List<FolderInfoModel>>("/api/Files/search-folder", formDataContent, arg2);
        return response.Data ?? [];
    }

    private Task FolderSearchChanged(FolderInfoModel obj)
    {
        SearchString = obj;
        return DataGrid.ReloadServerData();
    }
    
    private string ToStringFunc(FolderInfoModel? arg)
    {
        return arg?.ToString() ?? string.Empty;
    }
    
    private async Task AddNewFolder(MouseEventArgs obj)
    {
        var dialog = await DialogService.ShowAsync<AddNewFolderDialog>(AppLang.New_folder);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: string folderName })
        {
        }
    }

    public void Dispose()
    {
        DataGrid.Dispose();
    }

    private async Task<GridData<FolderInfoModel>> ServerReload(GridState<FolderInfoModel> arg)
    {
        Loading = true;
        await Task.Delay(1);
        return new GridData<FolderInfoModel>();
    }
}
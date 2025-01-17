using BusinessModels.System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.Drive.SampleFileUpload;

public partial class SampleFileUploadPage : ComponentBase
{
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
    private string _dragClass = DefaultDragClass;
    private readonly List<(string, double)> _fileNames = new();
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;

    private async Task ClearAsync()
    {
        await (_fileUpload?.ClearAsync() ?? Task.CompletedTask);
        _fileNames.Clear();
        ClearDragClass();
    }

    private Task OpenFilePickerAsync()
        => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;

    private void OnInputFileChanged(InputFileChangeEventArgs e)
    {
        ClearDragClass();
        var files = e.GetMultipleFiles();
        foreach (var file in files)
        {
            _fileNames.Add((file.Name, 0));
        }
    }

    private async Task Upload()
    {
        // Upload the files here
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
        Snackbar.Add("TODO: Upload your files!");
        int index = 0;
        if (_fileUpload != null && _fileUpload.Files != null)
        {
            using var multipartContent = new MultipartFormDataContent();
            foreach (var file in _fileUpload.Files)
            {
                await using var fileStream = file.OpenReadStream(long.MaxValue);
                var progress = new Progress<double>(percent =>
                {
                    _fileNames[index] = (_fileNames[index].Item1, percent);
                    InvokeAsync(StateHasChanged);
                });
                await using var progressStream = new ProgressStreamContent(fileStream, progress);
                var fileContent = new StreamContent(progressStream);
                multipartContent.Add(fileContent, _fileNames[index].Item1);
            }

            var folderRequest = await ApiService.GetFolderRequestAsync("", 0, 50, null, false, false);
            if (folderRequest.IsSuccessStatusCode)
            {
                var result = await ApiService.PostAsync($"api/files/upload-physical/{folderRequest.Data.Folder.AliasCode}", multipartContent);
                if (result.IsSuccessStatusCode)
                {
                    ToastService.ShowSuccess(result.Message, TypeClassList.ToastDefaultSetting);
                }
                else
                {
                    ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
                }
            }
        }
    }

    private void SetDragClass()
        => _dragClass = $"{DefaultDragClass} mud-border-primary";

    private void ClearDragClass()
        => _dragClass = DefaultDragClass;
}
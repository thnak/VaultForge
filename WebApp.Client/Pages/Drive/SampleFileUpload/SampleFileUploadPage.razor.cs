using BusinessModels.System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.Drive.SampleFileUpload;

public partial class SampleFileUploadPage(ILogger<SampleFileUploadPage> logger) : ComponentBase
{
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
    private string _dragClass = DefaultDragClass;
    private readonly List<UploadState> _fileNames = new();
    private IReadOnlyList<IBrowserFile> _fileUpload = [];

    private class UploadState
    {
        public string FileName { get; set; } = string.Empty;
        public double Progress { get; set; } = 0;
        public string Guid = System.Guid.NewGuid().ToString();
    }

    private Task ClearAsync()
    {
        _fileUpload = [];
        _fileNames.Clear();
        ClearDragClass();
        return Task.CompletedTask;
    }

    private Task OpenFilePickerAsync()
    {
        _fileUpload = [];
        return Task.CompletedTask;
    }

    private void OnInputFileChanged(InputFileChangeEventArgs e)
    {
        ClearDragClass();
        _fileUpload = e.GetMultipleFiles(Int32.MaxValue);
        _fileNames.Clear();
        foreach (var file in _fileUpload)
        {
            _fileNames.Add(new UploadState()
            {
                FileName = file.Name,
            });
        }
    }

    private async Task Upload()
    {
        int index = 0;

        using var multipartContent = new MultipartContent();
        List<Stream> streams = [];
        try
        {
            foreach (var file in _fileUpload)
            {
                var index1 = index;
                _fileNames[index1].Progress = 0;
                var progress = new Progress<double>(percent =>
                {
                    _fileNames[index1].Progress = percent;
                    InvokeAsync(StateHasChanged);
                });
                var progressStream = new ProgressStreamContent(file.OpenReadStream(long.MaxValue), progress);
                streams.Add(progressStream);
                var fileContent = new StreamContent(progressStream);
                multipartContent.Add(fileContent);
                index++;
            }

            var folderRequest = await ApiService.GetFolderRequestAsync(null, 0, 50, null, false, false);
            if (folderRequest.IsSuccessStatusCode)
            {
                var result = await ApiService.UploadFileAsync(folderRequest.Data.Folder.AliasCode, multipartContent);
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
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    private void SetDragClass()
        => _dragClass = $"{DefaultDragClass} mud-border-primary";

    private void ClearDragClass()
        => _dragClass = DefaultDragClass;
}
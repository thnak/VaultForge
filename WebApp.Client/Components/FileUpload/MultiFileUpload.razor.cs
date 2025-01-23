using BusinessModels.Service.Upload;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using WebApp.Client.Utils;
using Timer = System.Timers.Timer;

namespace WebApp.Client.Components.FileUpload;

public partial class MultiFileUpload(ILogger<MultiFileUpload> logger) : ComponentBase, IDisposable
{
    #region -- models --

    private class UploadProgress
    {
        public string FileName { get; set; } = string.Empty;
        public string Icon => ContentType.GetIconContentType();
        public string ContentType { get; set; } = string.Empty;
        public string UploadMessage { get; set; } = string.Empty;
        public string UploadSpeed { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public double Progress { get; set; } = 0;
        public Color ProgressColor { get; set; } = Color.Default;

        public readonly string Guid = System.Guid.NewGuid().ToString();

        public UploadState State { get; set; } = UploadState.None;
    }

    private enum UploadState
    {
        None,
        Uploading,
        Processing,
        Uploaded,
        Error,
    }

    #endregion

    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
    private string _dragClass = DefaultDragClass;
    private readonly List<UploadProgress> _fileNames = new();
    private List<IBrowserFile> _fileUpload = [];
    private Timer SpeedCheckTimer;
    private UploadSpeedService _speedService = new();

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SpeedCheckTimer = new Timer(1000) { AutoReset = true };
            _speedService.Trackers += Trackers;
        }

        base.OnAfterRender(firstRender);
    }

    private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

    private void ClearDragClass() => _dragClass = DefaultDragClass;

    private Task OnInputFileChanged(InputFileChangeEventArgs arg)
    {
        var selectedFiles = arg.GetMultipleFiles();
        _fileUpload.AddRange(selectedFiles);
        foreach (var file in selectedFiles)
        {
            var model = new UploadProgress
            {
                FileName = file.Name,
                UploadMessage = file.Name,
                ContentType = file.ContentType,
                FileSize = file.Size,
            };
            _fileNames.Add(model);
            _speedService.AddOrUpdate(model.Guid, 0);
        }

        return Upload(selectedFiles);
    }

    private Task Trackers()
    {
        if (_fileNames.All(x => x.State == UploadState.Uploaded))
            return Task.CompletedTask;

        foreach (var fileUpload in _fileNames)
        {
            fileUpload.UploadSpeed = _speedService.GetSpeedString(fileUpload.Guid);
        }

        return InvokeAsync(StateHasChanged);
    }

    private async Task Upload(IReadOnlyList<IBrowserFile> fileUploads)
    {
        int index = _fileNames.Count - fileUploads.Count;
        index = Math.Max(0, index);
        using var multipartContent = new MultipartContent();
        List<Stream> streams = [];
        try
        {
            foreach (var file in fileUploads)
            {
                var index1 = index;
                _fileNames[index1].Progress = 0;
                var progress = new Progress<double>(percent =>
                {
                    _fileNames[index1].Progress = percent;
                    _speedService.AddOrUpdate(_fileNames[index1].Guid, (long)(_fileNames[index1].FileSize * percent / 100));
                    _fileNames[index1].State = UploadState.Uploading;

                    if (percent >= 100)
                    {
                        _fileNames[index1].ProgressColor = Color.Secondary;
                        _fileNames[index1].State = UploadState.Processing;
                    }

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

                await Trackers();
                foreach (var file in _fileNames)
                {
                    file.ProgressColor = result.IsSuccessStatusCode ? Color.Success : Color.Error;
                    file.State = result.IsSuccessStatusCode ? UploadState.Uploaded : UploadState.Error;
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

    public void Dispose()
    {
        _speedService.Trackers -= Trackers;
        SpeedCheckTimer.Dispose();
        _speedService.Dispose();
    }
}
using Business.Services.Ffmpeg;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class FileSystemHandlerService : IFileSystemHandlerService
{
    public Task<string> ReadFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = FFmpegService.ExecuteCommand($"ffprobe -v quiet -print_format json -show_format -show_streams \"{filePath}\"");
        return Task.FromResult(result);
    }

    public Task<string> ExecuteCommand(string command, CancellationToken cancellationToken = default)
    {
        var result = FFmpegService.ExecuteCommand(command);
        return Task.FromResult(result);
    }
}
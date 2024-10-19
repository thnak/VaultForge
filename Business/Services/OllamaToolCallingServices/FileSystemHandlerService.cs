using Business.Services.Ffmpeg;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class FileSystemHandlerService : IFileSystemHandlerService
{
    public async Task<string> ReadFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = await TerminalExtension.ExecuteCommandAsync($"ffprobe -v quiet -print_format json -show_format -show_streams \"{filePath}\"", "", cancellationToken);
        return result;
    }

    public async Task<string> ExecuteCommand(string command, CancellationToken cancellationToken = default)
    {
        var result = await TerminalExtension.ExecuteCommandAsync(command, "", cancellationToken);
        return result;
    }
}
using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

// [OllamaTools]
public interface IFileSystemHandlerService
{
    [Description("read video stream information by FFMPEG")]
    public Task<string> ReadFileInfoAsync([Description("absolute file path")] string filePath, CancellationToken cancellationToken = default);
    
    [Description("excute command prompt")]
    public Task<string> ExecuteCommand([Description("command")] string command, CancellationToken cancellationToken = default);
}
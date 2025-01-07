using BrainNet.Models.Result;
using BrainNet.Service.ObjectDetection.Interfaces;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Implements;

public class YoloInferenceSessionService : IYoloInferenceSessionService
{
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(10); // Set inactivity timeout
    private DateTime _lastActivity = DateTime.UtcNow;

    private YoloInferenceService? _yoloInferenceService;


    public void Dispose()
    {
        _yoloInferenceService?.Dispose();
        _yoloInferenceService = null;
    }

    public bool IsExpired()
    {
        return (DateTime.UtcNow - _lastActivity) > _timeout;
    }

    public Task RunOneAsync(CancellationToken cancellationToken)
    {
        if (_yoloInferenceService != null) return _yoloInferenceService.RunOneAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public void Initialize(Stream modelStream)
    {
        using MemoryStream memoryStream = new();
        modelStream.CopyTo(memoryStream);
        _yoloInferenceService = new YoloInferenceService(memoryStream.ToArray(), TimeSpan.FromSeconds(1), 32, 0);
    }

    public Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        if (_yoloInferenceService != null)
        {
            _lastActivity = DateTime.UtcNow;
            return _yoloInferenceService.AddInputAsync(image, cancellationToken);
        }
        return Task.FromResult(InferenceResult<List<YoloBoundingBox>>.Failure("Disposed service", InferenceErrorType.PermissionDenied));
    }
}
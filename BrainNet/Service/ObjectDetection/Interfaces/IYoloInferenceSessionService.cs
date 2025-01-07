using BrainNet.Models.Result;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloInferenceSessionService : IDisposable
{
    void Initialize(Stream modelStream);
    bool IsExpired();
    Task RunAsync(CancellationToken cancellationToken);
    Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default);
}
using BrainNet.Models.Result;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloInferenceService : IDisposable
{

    public int GetBatchSize();
    /// <summary>
    /// return a TaskCompletionSource
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default);

    Task RunAsync(CancellationToken cancellationToken);

}
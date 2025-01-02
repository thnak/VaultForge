using BrainNet.Models.Result;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloInferenceService : IDisposable
{

    /// <summary>
    /// return a TaskCompletionSource
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image);

    Task RunAsync(CancellationToken cancellationToken);

}
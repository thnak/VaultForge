using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.FaceEmbedding.Interfaces;

public interface IFaceEmbedding : IDisposable
{
    public int GetBatchSize();

    /// <summary>
    /// return a TaskCompletionSource
    /// </summary>
    /// <param name="image"></param>
    /// <param name="weights"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<float[]> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default);

    Task RunAsync(CancellationToken cancellationToken);
    Task RunOneAsync(CancellationToken cancellationToken);
}
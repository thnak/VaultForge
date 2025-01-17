using BrainNet.Service.FaceEmbedding.Implements;
using BrainNet.Service.FaceEmbedding.Interfaces;
using Business.Services.Configure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Business.Services.OnnxService.Face;

public interface IFaceEmbeddingInferenceService : IFaceEmbedding;

public class FaceEmbeddingInferenceService(ApplicationConfiguration configuration) : IFaceEmbeddingInferenceService
{
    public FaceEmbedding FaceEmbedding = new(configuration.GetBrainNetSetting.FaceEmbeddingSetting.FaceEmbeddingPath,
        TimeSpan.FromMilliseconds(configuration.GetBrainNetSetting.FaceEmbeddingSetting.PeriodicTimer),
        configuration.GetBrainNetSetting.FaceEmbeddingSetting.MaxQueSize,
        configuration.GetBrainNetSetting.FaceEmbeddingSetting.DeviceIndex);

    public void Dispose()
    {
        FaceEmbedding.Dispose();
    }

    public int GetBatchSize()
    {
        return FaceEmbedding.GetBatchSize();
    }

    public Task<float[]> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        return FaceEmbedding.AddInputAsync(image, cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return FaceEmbedding.RunAsync(cancellationToken);
    }

    public Task RunOneAsync(CancellationToken cancellationToken)
    {
        return FaceEmbedding.RunOneAsync(cancellationToken);
    }
}
using Business.Services.OnnxService.Face;
using Microsoft.Extensions.Hosting;

namespace Business.Services.HostedServices.Face;

public class FaceEmbeddingInferenceHostedService(IFaceEmbeddingInferenceService faceEmbeddingInferenceService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return faceEmbeddingInferenceService.RunAsync(stoppingToken);
    }
}
using BrainNet.Models.Result;
using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Interfaces;
using BrainNet.Service.ObjectDetection.Model.Result;
using Business.Services.Configure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Business.Services.OnnxService.WaterMeter;

public interface IWaterMeterInferenceService : IYoloInferenceService
{
}

public class WaterMeterInferenceService(ApplicationConfiguration configuration) : IWaterMeterInferenceService
{
    public YoloInferenceService YoloInferenceService = new(configuration.GetBrainNetSetting.WaterSetting.DetectionPath,
        TimeSpan.FromMilliseconds(configuration.GetBrainNetSetting.WaterSetting.PeriodicTimer),
        configuration.GetBrainNetSetting.WaterSetting.MaxQueSize,
        configuration.GetBrainNetSetting.WaterSetting.DeviceIndex);

    public Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        return YoloInferenceService.AddInputAsync(image, cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return YoloInferenceService.RunAsync(cancellationToken);
    }

    public void Dispose()
    {
        YoloInferenceService.Dispose();
    }
}
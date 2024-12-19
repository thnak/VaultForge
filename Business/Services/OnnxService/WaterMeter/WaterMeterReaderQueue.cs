using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.WaterMeter.Implements;
using BrainNet.Service.WaterMeter.Interfaces;
using Business.Business.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Services.Configure;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Business.Services.OnnxService.WaterMeter;

public interface IWaterMeterReaderQueue : IDisposable
{
    public Task<int> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default);
}

public class WaterMeterReaderQueue(ApplicationConfiguration configuration, ILogger<IWaterMeterReaderQueue> logger, RedundantArrayOfIndependentDisks disks, IFileSystemBusinessLayer fileSystemBusinessLayer) : IWaterMeterReaderQueue
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IWaterMeterReader _waterMeterReader = new WaterMeterReader(configuration.GetOnnxConfig.WaterMeterWeightPath);
    private readonly MemoryStream _memoryStream = new(16 * 1024 * 1024);

    public async Task<int> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var file = fileSystemBusinessLayer.Get(record.Metadata.ImagePath);
            if (file is null)
            {
                logger.LogWarning($"image not found: {record.Metadata.ImagePath}");
                return 0;
            }

            _memoryStream.SetLength(0);
            await disks.ReadGetDataAsync(_memoryStream, file.AbsolutePath, cancellationToken);
            _memoryStream.Seek(0, SeekOrigin.Begin);
            var feed = new YoloFeeder(_waterMeterReader.GetInputDimensions()[2..], _waterMeterReader.GetStride());
            var image = await Image.LoadAsync<Rgb24>(_memoryStream, cancellationToken);
            feed.SetTensor(image);
            var result = _waterMeterReader.PredictWaterMeter(feed);

            return result;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _waterMeterReader.Dispose();
        _memoryStream.Dispose();
    }
}
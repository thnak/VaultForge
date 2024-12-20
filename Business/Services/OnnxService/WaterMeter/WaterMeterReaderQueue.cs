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

public class WaterMeterReaderQueue : IWaterMeterReaderQueue
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IWaterMeterReader _waterMeterReader;
    private readonly MemoryStream _memoryStream = new(16 * 1024 * 1024);
    private readonly YoloFeeder _feeder;
    private readonly ILogger<IWaterMeterReaderQueue> _logger;
    private readonly RedundantArrayOfIndependentDisks _redundantArrayOfIndependentDisks;
    private readonly IFileSystemBusinessLayer _fileSystemBusinessLayer;
    private int Count { get; set; }

    public WaterMeterReaderQueue(ApplicationConfiguration configuration, ILogger<IWaterMeterReaderQueue> logger, RedundantArrayOfIndependentDisks disks, IFileSystemBusinessLayer fileSystemBusinessLayer)
    {
        _waterMeterReader = new WaterMeterReader(configuration.GetOnnxConfig.WaterMeterWeightPath);
        _feeder = new YoloFeeder(_waterMeterReader.GetInputDimensions()[2..], _waterMeterReader.GetStride());
        _logger = logger;
        _redundantArrayOfIndependentDisks = disks;
        _fileSystemBusinessLayer = fileSystemBusinessLayer;
    }


    public async Task<int> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var file = _fileSystemBusinessLayer.Get(record.Metadata.ImagePath);
            if (file is null)
            {
                _logger.LogWarning($"image not found: {record.Metadata.ImagePath}");
                return 0;
            }

            if (file.FileSize > _memoryStream.Capacity)
            {
                _logger.LogWarning($"image size is too big: {record.Metadata.ImagePath}");
                return 0;
            }
            _memoryStream.SetLength(0);
            await _redundantArrayOfIndependentDisks.ReadGetDataAsync(_memoryStream, file.AbsolutePath, cancellationToken);
            var image = await Image.LoadAsync<Rgb24>(_memoryStream, cancellationToken);
            _feeder.SetTensor(image);
            Count++;

            var result = _waterMeterReader.PredictWaterMeter(_feeder);
            _feeder.Clear();
            return result[0];
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
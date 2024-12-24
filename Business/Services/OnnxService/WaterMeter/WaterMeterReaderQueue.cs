using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.WaterMeter.Implements;
using BrainNet.Service.WaterMeter.Interfaces;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.StorageSpace;
using Business.Services.Configure;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
    private readonly IIotRecordBusinessLayer _recordBusinessLayer;
    private readonly IIoTSensorBusinessLayer _iotSensorBusinessLayer;
    private int Count { get; set; }

    public WaterMeterReaderQueue(ApplicationConfiguration configuration, ILogger<IWaterMeterReaderQueue> logger,
        RedundantArrayOfIndependentDisks disks, IFileSystemBusinessLayer fileSystemBusinessLayer, IIotRecordBusinessLayer recordBusinessLayer,
        IIoTSensorBusinessLayer iotSensorBusinessLayer)
    {
        _waterMeterReader = new WaterMeterReader(configuration.GetOnnxConfig.WaterMeterWeightPath);
        _feeder = new YoloFeeder(_waterMeterReader.GetInputDimensions()[2..], _waterMeterReader.GetStride());
        _logger = logger;
        _redundantArrayOfIndependentDisks = disks;
        _fileSystemBusinessLayer = fileSystemBusinessLayer;
        _recordBusinessLayer = recordBusinessLayer;
        _iotSensorBusinessLayer = iotSensorBusinessLayer;
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

            var sensor = _iotSensorBusinessLayer.Get(record.Metadata.SensorId);


            _memoryStream.SetLength(0);
            await _redundantArrayOfIndependentDisks.ReadGetDataAsync(_memoryStream, file.AbsolutePath, cancellationToken);
            using var image = await Image.LoadAsync<Rgb24>(_memoryStream, cancellationToken);

            if (sensor is { Rotate: > 0 })
                image.Mutate(i => i.Rotate(sensor.Rotate));

            _feeder.SetTensor(image);
            Count++;

            var result = _waterMeterReader.PredictWaterMeter(_feeder);
            _feeder.Clear();
            if (!result.Any())
            {
                result.Add(0);
            }

            await _recordBusinessLayer.UpdateIotValue(record.Id.ToString(), result[0], ProcessStatus.Completed, cancellationToken);
            return result[0];
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
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
﻿using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.StorageSpace;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Business.Services.OnnxService.WaterMeter;

public interface IWaterMeterReaderQueue : IDisposable
{
    public Task<IoTRecordUpdateModel> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default);
}

public class WaterMeterReaderQueue : IWaterMeterReaderQueue
{
    private readonly ILogger<IWaterMeterReaderQueue> _logger;
    private readonly RedundantArrayOfIndependentDisks _redundantArrayOfIndependentDisks;
    private readonly IFileSystemBusinessLayer _fileSystemBusinessLayer;
    private readonly IIoTSensorBusinessLayer _iotSensorBusinessLayer;
    private readonly IWaterMeterInferenceService _waterMeterInferenceService;
    private readonly SemaphoreSlim _semaphore;

    public WaterMeterReaderQueue(ILogger<IWaterMeterReaderQueue> logger,
        RedundantArrayOfIndependentDisks disks, IFileSystemBusinessLayer fileSystemBusinessLayer,
        IIoTSensorBusinessLayer iotSensorBusinessLayer,
        IWaterMeterInferenceService waterMeterInferenceService)
    {
        _logger = logger;
        _redundantArrayOfIndependentDisks = disks;
        _fileSystemBusinessLayer = fileSystemBusinessLayer;
        _iotSensorBusinessLayer = iotSensorBusinessLayer;
        _waterMeterInferenceService = waterMeterInferenceService;
        _semaphore = new(_waterMeterInferenceService.GetBatchSize() * 8);
    }


    public async Task<IoTRecordUpdateModel> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var file = _fileSystemBusinessLayer.Get(record.Metadata.ImagePath);
            if (file is null)
            {
                _logger.LogWarning($"image not found: {record.Metadata.ImagePath}");
                return new IoTRecordUpdateModel()
                {
                    SensorId = record.Metadata.SensorId,
                    RecordedAt = record.Metadata.RecordedAt,
                    ProcessStatus = ProcessStatus.Failed
                };
            }

            var sensor = _iotSensorBusinessLayer.Get(record.Metadata.SensorId);

            var pathArray = await _redundantArrayOfIndependentDisks.GetDataBlockPaths(file.AbsolutePath, CancellationToken.None);
            if (pathArray == null)
            {
                _logger.LogWarning($"image not found in RAID: {record.Metadata.ImagePath}");
                return new IoTRecordUpdateModel()
                {
                    SensorId = record.Metadata.SensorId,
                    RecordedAt = record.Metadata.RecordedAt,
                    ProcessStatus = ProcessStatus.Failed
                };
            }

            using MemoryStream memoryStream = new MemoryStream();
            await using Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files, pathArray.FileSize, pathArray.StripeSize, FileMode.Open, FileAccess.Read, FileShare.Read);
            await raid5Stream.CopyToAsync(memoryStream, (int)pathArray.FileSize, CancellationToken.None);
            memoryStream.Seek(0, SeekOrigin.Begin);

            try
            {
                using var image = await Image.LoadAsync<Rgb24>(memoryStream, CancellationToken.None);
                image.Mutate(i =>
                {
                    i.AutoOrient();
                    if (sensor is { Rotate: > 0 })
                        i.Rotate(sensor.Rotate);
                    if (sensor is { FlipHorizontal: true })
                        i.Flip(FlipMode.Vertical);
                    if (sensor is { FlipVertical: true })
                        i.Flip(FlipMode.Horizontal);
                });
                
                var predResult = await _waterMeterInferenceService.AddInputAsync(image, cancellationToken);

                if (predResult.IsSuccess)
                {
                    var resultString = string.Join("", predResult.Value.OrderBy(x => x.X).Select(x => x.ClassIdx.ToString()));
                    float.TryParse(resultString, out var result);
                    return new IoTRecordUpdateModel()
                    {
                        SensorId = record.Metadata.SensorId,
                        RecordedAt = record.Metadata.RecordedAt,
                        ProcessStatus = ProcessStatus.Completed,
                        SensorData = result
                    };
                }

                return new IoTRecordUpdateModel()
                {
                    SensorId = record.Metadata.SensorId,
                    RecordedAt = record.Metadata.RecordedAt,
                    ProcessStatus = ProcessStatus.Failed
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while processing image");
                return new IoTRecordUpdateModel()
                {
                    SensorId = record.Metadata.SensorId,
                    RecordedAt = record.Metadata.RecordedAt,
                    ProcessStatus = ProcessStatus.Failed
                };
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }

        return new IoTRecordUpdateModel()
        {
            SensorId = record.Metadata.SensorId,
            RecordedAt = record.Metadata.RecordedAt,
            ProcessStatus = ProcessStatus.Failed
        };
    }

    public void Dispose()
    {
        //
    }
}
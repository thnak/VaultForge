﻿using System.Buffers;
using BrainNet.Utils;
using Business.Business.Interfaces.FileSystem;
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
    private readonly ArrayPool<byte> _memoryPool = ArrayPool<byte>.Shared;

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
    }


    public async Task<IoTRecordUpdateModel> GetWaterMeterReadingCountAsync(IoTRecord record, CancellationToken cancellationToken = default)
    {
        var file = _fileSystemBusinessLayer.Get(record.Metadata.ImagePath);
        if (file is null)
        {
            _logger.LogWarning($"image not found: {record.Metadata.ImagePath}");
            return new IoTRecordUpdateModel()
            {
                SensorId = record.Metadata.SensorId,
                ProcessStatus = ProcessStatus.Failed
            };
        }

        var buffer = _memoryPool.Rent((int)file.FileSize);

        try
        {
            var sensor = _iotSensorBusinessLayer.Get(record.Metadata.SensorId);
            await _redundantArrayOfIndependentDisks.ReadGetDataAsync(buffer, file.AbsolutePath, cancellationToken);
            using var image = Image.Load<Rgb24>(buffer);
            image.AutoOrient();
            if (sensor is { Rotate: > 0 })
                image.Mutate(i => i.Rotate(sensor.Rotate));
            if (sensor is { FlipHorizontal: true })
                image.Mutate(i => i.Flip(FlipMode.Horizontal));
            if (sensor is { FlipVertical: true })
                image.Mutate(i => i.Flip(FlipMode.Vertical));


            var predResult = await _waterMeterInferenceService.AddInputAsync(image, cancellationToken);
            if (predResult.IsSuccess)
            {
                var resultString = string.Join("", predResult.Value.OrderBy(x => x.X).Select(x => x.ClassIdx.ToString()));
                float.TryParse(resultString, out var result);
                return new IoTRecordUpdateModel()
                {
                    SensorId = record.Metadata.SensorId,
                    ProcessStatus = ProcessStatus.Completed,
                    SensorData = result
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
            _memoryPool.Return(buffer);
        }

        return new IoTRecordUpdateModel()
        {
            SensorId = record.Metadata.SensorId,
            ProcessStatus = ProcessStatus.Failed
        };
    }

    public void Dispose()
    {
        //
    }
}
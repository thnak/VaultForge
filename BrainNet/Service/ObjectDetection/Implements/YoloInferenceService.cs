﻿using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;
using BrainNet.Models.Result;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using BrainNet.Service.Font.Implements;
using BrainNet.Service.Font.Interfaces;
using BrainNet.Service.Memory.Implements;
using BrainNet.Service.Memory.Interfaces;
using BrainNet.Service.Memory.Utils;
using BrainNet.Service.ObjectDetection.Interfaces;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;
using BrainNet.Utils;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FontFamily = BrainNet.Service.Font.Model.FontFamily;

namespace BrainNet.Service.ObjectDetection.Implements;

public class YoloInferenceService : IYoloInferenceService
{
    private readonly TimeSpan _timeout;
    private readonly Channel<(YoloInferenceServiceFeeder feeder, TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>> tcs)> _inputChannel;
    private readonly InferenceSession _session;

    private readonly IMemoryAllocatorService _memoryAllocatorService = new MemoryAllocatorService();

    private readonly IFontServiceProvider _fontServiceProvider = new FontServiceProvider();

    // private IOptions<BrainNetSettingModel>? Options { get; }
    private string[] InputNames { get; set; } = null!;
    private string[] OutputNames { get; set; } = null!;
    public int[] InputDimensions { get; set; } = [];
    public long[] OutputDimensions { get; set; } = [];
    private float[] InputFeedBuffer { get; set; }
    private bool[] InferenceStates { get; set; }
    public string[] CategoryReadOnlyCollection { get; set; } = [];
    public int Stride { get; set; }
    private readonly RunOptions _runOptions;
    private TensorShape _tensorShape;
    private TensorShape _inputTensorShape;
    private Size _inputSize;
    private readonly ArrayPool<float> _floatPool = ArrayPool<float>.Create();
    private readonly ArrayPool<bool> _boolPool = ArrayPool<bool>.Create();
    public SixLabors.Fonts.Font PrimaryFont;

    private readonly int _singleInputLength;

    public YoloInferenceService(string modelPath, TimeSpan timeout, int maxQueueSize, int deviceIndex)
    {
        var sessionOption = InitSessionOption(deviceIndex);
        _session = new InferenceSession(modelPath, sessionOption);
        // Options = new OptionsWrapper<BrainNetSettingModel>(settings);
        InitializeSession();
        InitCategory();
        _runOptions = new();
        PrimaryFont = _fontServiceProvider.CreateFont(FontFamily.RobotoRegular, 14, FontStyle.Regular);
        _timeout = timeout;
        var inputLength = 1;
        for (int i = 0; i < InputDimensions.Length; i++)
        {
            inputLength *= InputDimensions[i];
        }

        _singleInputLength = inputLength / InputDimensions[0];

        InputFeedBuffer = _floatPool.Rent(inputLength);
        InferenceStates = _boolPool.Rent(InputDimensions[0]);
        _inputChannel = Channel.CreateBounded<(YoloInferenceServiceFeeder feeder, TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>> tcs)>(new BoundedChannelOptions(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
        });
    }

    public YoloInferenceService(IOptions<BrainNetSettingModel> options)
    {
        var sessionOption = InitSessionOption(options.Value.WaterSetting.DeviceIndex);
        _session = new InferenceSession(options.Value.WaterSetting.DetectionPath, sessionOption);
        // Options = options;
        InitializeSession();
        InitCategory();
        _runOptions = new();
        PrimaryFont = _fontServiceProvider.CreateFont(FontFamily.RobotoRegular, 14, FontStyle.Regular);
        _timeout = TimeSpan.FromSeconds(options.Value.WaterSetting.PeriodicTimer);
        var inputLength = 1;
        for (int i = 0; i < InputDimensions.Length; i++)
        {
            inputLength *= InputDimensions[i];
        }

        _singleInputLength = inputLength / InputDimensions[0];

        InferenceStates = _boolPool.Rent(InputDimensions[0]);
        InputFeedBuffer = _floatPool.Rent(inputLength);
        _inputChannel = Channel.CreateBounded<(YoloInferenceServiceFeeder feeder, TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>> tcs)>(new BoundedChannelOptions(options.Value.WaterSetting.MaxQueSize)
        {
            FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
        });
    }


    #region Init

    private void InitializeSession()
    {
        InputNames = _session.GetInputNames();
        OutputNames = _session.GetOutputNames();
        InputDimensions = _session.InputMetadata.First().Value.Dimensions;
        OutputDimensions = [.._session.OutputMetadata.First().Value.Dimensions];
        _tensorShape = new TensorShape(InputDimensions);
        _inputTensorShape = new TensorShape([1, InputDimensions[1], InputDimensions[2], InputDimensions[3]]);
        _inputSize = new Size(InputDimensions[^1], InputDimensions[^2]);
    }

    private SessionOptions InitSessionOption(int deviceIndex)
    {
        var sessionOptions = new SessionOptions();
        // sessionOptions.RegisterOrtExtensions();
        sessionOptions.InitSessionOption();
        sessionOptions.InitExecutionProviderOptions(deviceIndex);
        return sessionOptions;
    }

    private void InitCategory()
    {
        var metadata = _session.ModelMetadata;
        var customMetadata = metadata.CustomMetadataMap;
        if (customMetadata.TryGetValue("names", out var categories))
        {
            List<string> list = [];
            if (categories != null)
            {
                try
                {
                    var content = JsonConvert.DeserializeObject<List<string>>(categories);

                    if (content != null) list = content;
                    else
                    {
                        list = new List<string>();
                        for (var i = 0; i < 10000; i++)
                        {
                            list.Add($"Named[ {i} ]");
                        }
                    }
                }
                catch
                {
                    list = new List<string>();
                    for (var i = 0; i < 10000; i++)
                    {
                        list.Add($"Named[ {i} ]");
                    }
                }
            }

            CategoryReadOnlyCollection = list.ToArray();
        }

        if (customMetadata.TryGetValue("stride", out var strideString))
        {
            List<float>? strides = JsonConvert.DeserializeObject<List<float>>(strideString);
            if (strides != null)
            {
                Stride = strides.Any() ? (int)strides.Max() : 32;
            }
        }
    }

    #endregion

    public async Task<InferenceResult<List<YoloBoundingBox>>> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        while (await _inputChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            var tcs = new TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>>();
            using MemoryTensorOwner<float> memoryTensorOwner = _memoryAllocatorService.AllocateTensor(_inputTensorShape, true);
            var pads = _floatPool.Rent(1);
            var ratios = _floatPool.Rent(1);
            image.NormalizeInput(memoryTensorOwner.Tensor, _inputSize, ratios, pads, true);
            var buffer = _floatPool.Rent(_singleInputLength);
            memoryTensorOwner.Tensor.Span.CopyTo(buffer);
            YoloInferenceServiceFeeder feeder = new YoloInferenceServiceFeeder(buffer)
            {
                OriginImageHeight = image.Height,
                OriginImageWidth = image.Width,
                HeightRatio = ratios[0],
                WidthRatio = ratios[1],
                PadHeight = pads[0],
                PadWidth = pads[1],
            };
            _floatPool.Return(pads);
            _floatPool.Return(ratios);
            try
            {
                await _inputChannel.Writer.WriteAsync((feeder, tcs), cancellationToken);
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _floatPool.Return(buffer);
                tcs.SetResult(InferenceResult<List<YoloBoundingBox>>.Canceled("Canceled"));
                return await tcs.Task;
            }
        }

        return InferenceResult<List<YoloBoundingBox>>.Failure("Failed to add image", InferenceErrorType.PermissionDenied);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<(YoloInferenceServiceFeeder feeder, TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>> tcs)>();
        var sw = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            while (buffer.Count < InputDimensions[0] && sw.Elapsed < _timeout)
            {
                if (_inputChannel.Reader.TryRead(out var item))
                {
                    buffer.Add(item);
                }
                else
                {
                    await Task.Delay(100, cancellationToken); // Small delay to prevent busy-waiting
                }
            }

            sw.Restart();

            if (buffer.Count > 0)
            {
                ProcessBatchAsync(buffer);
                buffer.Clear();
            }
        }
    }

    private void ProcessBatchAsync(List<(YoloInferenceServiceFeeder, TaskCompletionSource<InferenceResult<List<YoloBoundingBox>>>)> batch)
    {
        var batchSize = batch.Count;
        // Copy inputs into the batched array

        for (int i = 0; i < batchSize; i++)
        {
            InferenceStates[i] = false;
            var inputSize = batch[i].Item1.Buffer.Length;
            Array.Copy(batch[i].Item1.Buffer, 0, InputFeedBuffer, i * inputSize, inputSize);
            _floatPool.Return(batch[i].Item1.Buffer);
        }

        try
        {
            // Run inference
            using var ortInput = InputFeedBuffer.CreateOrtValue(_tensorShape.Dimensions64);

            var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), ortInput } };

            using var results = _session.Run(_runOptions, inputs, OutputNames);

            var predictSpan = results[0].Value.GetTensorDataAsSpan<float>();
            var predictArray = _floatPool.Rent(predictSpan.Length);
            predictSpan.CopyTo(predictArray);

            var pads = batch.Select(x => new[] { x.Item1.PadHeight, x.Item1.PadWidth }).ToList();
            var ratios = batch.Select(x => new[] { x.Item1.HeightRatio, x.Item1.WidthRatio }).ToList();
            var originShape = batch.Select(x => new[] { x.Item1.OriginImageHeight, x.Item1.OriginImageWidth }).ToList();


#if DEBUG
            // predictions.
#endif
            YoloPrediction predictions = new YoloPrediction(predictArray, CategoryReadOnlyCollection, pads, ratios, originShape);

            foreach (var batchResult in predictions.GetDetect().GroupBy(x => x.BatchId))
            {
                try
                {
                    var resultList = batchResult.ToList();
                    batch[batchResult.Key].Item2.SetResult(InferenceResult<List<YoloBoundingBox>>.Success(resultList));
                    InferenceStates[batchResult.Key] = true;
                }
                catch (Exception)
                {
                    InferenceStates[batchResult.Key] = false;
                }
            }


            for (int i = 0; i < batchSize; i++)
            {
                if (InferenceStates[i] == false)
                {
                    batch[i].Item2.SetResult(InferenceResult<List<YoloBoundingBox>>.Success([]));
                }
            }

            _floatPool.Return(predictArray);
            Array.Clear(InputFeedBuffer);
        }
        catch (Exception exception)
        {
            for (int i = 0; i < batchSize; i++)
            {
                batch[i].Item2.SetResult(InferenceResult<List<YoloBoundingBox>>.Failure(exception.Message, InferenceErrorType.Unknown));
            }
        }
        finally
        {
        }
    }

    public void Dispose()
    {
        _floatPool.Return(InputFeedBuffer);
        _boolPool.Return(InferenceStates);
        _session.Dispose();
        _memoryAllocatorService.Dispose();
        _runOptions.Dispose();
    }
}
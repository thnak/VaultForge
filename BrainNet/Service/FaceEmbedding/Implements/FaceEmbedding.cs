using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using BrainNet.Service.FaceEmbedding.Interfaces;
using BrainNet.Service.Font.Implements;
using BrainNet.Service.Font.Interfaces;
using BrainNet.Service.Memory.Implements;
using BrainNet.Service.Memory.Interfaces;
using BrainNet.Service.Memory.Utils;
using BrainNet.Service.ObjectDetection;
using BrainNet.Utils;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FontFamily = BrainNet.Service.Font.Model.FontFamily;

namespace BrainNet.Service.FaceEmbedding.Implements;

public class FaceEmbedding : IFaceEmbedding
{
    private readonly TimeSpan _timeout;
    private readonly Channel<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)> _inputChannel;
    private readonly InferenceSession _session;

    private readonly IMemoryAllocatorService _memoryAllocatorService;

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
    private readonly ArrayPool<float> _singleFrameInputArrayPool;
    private readonly ArrayPool<float> _padAndRatiosArrayPool;
    private readonly ArrayPool<bool> _boolPool = ArrayPool<bool>.Create();
    private readonly int _singleInputLength;
    public SixLabors.Fonts.Font PrimaryFont;

    #region -- init service --

    public FaceEmbedding(string modelPath, TimeSpan timeout, int maxQueueSize, int deviceIndex)
    {
        var sessionOption = InitSessionOption(deviceIndex);
        _session = new InferenceSession(modelPath, sessionOption);
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
        _singleFrameInputArrayPool = ArrayPool<float>.Create(_singleInputLength, (int)(maxQueueSize * 1.5));
        _padAndRatiosArrayPool = ArrayPool<float>.Create(1, maxQueueSize * 2);
        _memoryAllocatorService = new MemoryAllocatorService(_singleInputLength, maxQueueSize * 2);

        InputFeedBuffer = ArrayPool<float>.Shared.Rent(inputLength);
        InferenceStates = _boolPool.Rent(InputDimensions[0]);
        _inputChannel = Channel.CreateBounded<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)>(
            new BoundedChannelOptions(maxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
            });
    }

    public FaceEmbedding(byte[] modelWeight, TimeSpan timeout, int maxQueueSize, int deviceIndex)
    {
        var sessionOption = InitSessionOption(deviceIndex);
        _session = new InferenceSession(modelWeight, sessionOption);
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
        _singleFrameInputArrayPool = ArrayPool<float>.Create(_singleInputLength, (int)(maxQueueSize * 1.5));
        _padAndRatiosArrayPool = ArrayPool<float>.Create(1, maxQueueSize * 2);
        _memoryAllocatorService = new MemoryAllocatorService(_singleInputLength, maxQueueSize * 2);

        InputFeedBuffer = ArrayPool<float>.Shared.Rent(inputLength);
        InferenceStates = _boolPool.Rent(InputDimensions[0]);
        _inputChannel = Channel.CreateBounded<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)>(
            new BoundedChannelOptions(maxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
            });
    }

    public FaceEmbedding(IOptions<BrainNetSettingModel> options)
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
        _singleFrameInputArrayPool = ArrayPool<float>.Create(_singleInputLength, (int)(options.Value.WaterSetting.MaxQueSize * 1.5));
        _padAndRatiosArrayPool = ArrayPool<float>.Create(1, options.Value.WaterSetting.MaxQueSize * 2);
        _memoryAllocatorService = new MemoryAllocatorService(_singleInputLength);
        InputFeedBuffer = _singleFrameInputArrayPool.Rent(inputLength);
        _inputChannel = Channel.CreateBounded<(MemoryTensorOwner<float> feeder,
            TaskCompletionSource<float[]> tcs)>(new BoundedChannelOptions(options.Value.WaterSetting.MaxQueSize)
        {
            FullMode = BoundedChannelFullMode.Wait // Wait when the channel is full
        });
    }

    #endregion


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

    public int GetBatchSize()
    {
        return InputDimensions[0];
    }

    public async Task<float[]> AddInputAsync(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        while (await _inputChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            var tcs = new TaskCompletionSource<float[]>();
            MemoryTensorOwner<float> memoryTensorOwner = _memoryAllocatorService.AllocateTensor(_inputTensorShape, true);
            var pads = _padAndRatiosArrayPool.Rent(2);
            var ratios = _padAndRatiosArrayPool.Rent(2);
            memoryTensorOwner.Tensor.Span.Fill(114 / 255f);
            image.NormalizeInput(memoryTensorOwner.Tensor, _inputSize, ratios, pads, true);
            _padAndRatiosArrayPool.Return(pads, true);
            _padAndRatiosArrayPool.Return(ratios, true);

            try
            {
                await _inputChannel.Writer.WriteAsync((memoryTensorOwner, tcs), cancellationToken);
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                tcs.SetResult([]);
                return await tcs.Task;
            }
        }

        return [];
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)>();
        var sw = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            await SelfRunOneAsync(sw, buffer, cancellationToken);
            buffer.Clear();
        }
    }


    public async Task RunOneAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)>();
        var sw = Stopwatch.StartNew();
        await SelfRunOneAsync(sw, buffer, cancellationToken);
    }

    private async Task SelfRunOneAsync(Stopwatch sw, List<(MemoryTensorOwner<float> feeder, TaskCompletionSource<float[]> tcs)> buffer, CancellationToken cancellationToken)
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
        }
    }

    private void ProcessBatchAsync(List<(MemoryTensorOwner<float>, TaskCompletionSource<float[]>)> batch)
    {
        Array.Clear(InputFeedBuffer);
        var batchSize = batch.Count;

        for (int i = 0; i < batchSize; i++)
        {
            InferenceStates[i] = false;
            batch[i].Item1.Tensor.Buffer.Span.CopyTo(InputFeedBuffer.AsSpan(i * _singleInputLength, batch[i].Item1.Tensor.Buffer.Span.Length));
            batch[i].Item1.Dispose();
        }

        // Run inference
        using var ortInput = InputFeedBuffer.CreateOrtValue(_tensorShape.Dimensions64);
        var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), ortInput } };
        using var results = _session.Run(_runOptions, inputs, OutputNames);
        var predictSpan = results[0].Value.GetTensorDataAsSpan<float>();
        var predictArray = _singleFrameInputArrayPool.Rent(predictSpan.Length);
        predictSpan.CopyTo(predictArray);

        var outPutSize = (int)OutputDimensions[1];
        for (int i = 0; i < batchSize; i++)
        {
            var spanR = predictArray.AsSpan(i * outPutSize, outPutSize);
            batch[i].Item2.SetResult(spanR.ToArray());
        }

        _singleFrameInputArrayPool.Return(predictArray, true);
    }

    public void Dispose()
    {
        ArrayPool<float>.Shared.Return(InputFeedBuffer, true);
        _boolPool.Return(InferenceStates, true);
        _session.Dispose();
        _memoryAllocatorService.Dispose();
        _runOptions.Dispose();
    }
}
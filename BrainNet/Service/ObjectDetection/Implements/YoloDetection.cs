using System.Buffers;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Implements;

public class YoloDetection : IYoloDetection
{
    private readonly IMemoryAllocatorService _memoryAllocatorService = new MemoryAllocatorService();
    private readonly InferenceSession _session;
    private IOptions<BrainNetSettingModel> Options { get; }
    private string[] InputNames { get; set; } = null!;
    private string[] OutputNames { get; set; } = null!;
    public int[] InputDimensions { get; set; } = [];
    public long[] OutputDimensions { get; set; } = [];
    public IReadOnlyCollection<string> CategoryReadOnlyCollection { get; set; } = [];
    public int Stride { get; set; }
    private OrtIoBinding OrtIoBinding { get; set; }
    private readonly RunOptions _runOptions;
    private TensorShape _tensorShape;
    private Size inputSize;
    private ArrayPool<float> floatPool = ArrayPool<float>.Create();

    public YoloDetection(string modelPath)
    {
        var settings = new BrainNetSettingModel()
        {
            FaceEmbeddingSetting = new FaceEmbeddingSettingModel
            {
                FaceEmbeddingPath = modelPath,
                DeviceIndex = 0,
            }
        };
        var sessionOption = InitSessionOption();
        _session = new InferenceSession(modelPath, sessionOption);
        OrtIoBinding = _session.CreateIoBinding();
        Options = new OptionsWrapper<BrainNetSettingModel>(settings);
        InitializeSession();
        InitCategory();
        _runOptions = new();
    }

    private void InitializeSession()
    {
        InputNames = _session.GetInputNames();
        OutputNames = _session.GetOutputNames();
        InputDimensions = _session.InputMetadata.First().Value.Dimensions;
        OutputDimensions = [.._session.OutputMetadata.First().Value.Dimensions];
        _tensorShape = new TensorShape(InputDimensions);
        inputSize = new Size(InputDimensions[^1], InputDimensions[^2]);
    }

    private SessionOptions InitSessionOption()
    {
        var sessionOptions = new SessionOptions();
        // sessionOptions.RegisterOrtExtensions();
        sessionOptions.InitSessionOption();
        // sessionOptions.InitExecutionProviderOptions(Options.Value.FaceEmbeddingSetting.DeviceIndex);
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

    public void Dispose()
    {
        _session.Dispose();
        _memoryAllocatorService.Dispose();
    }

    public int[] GetInputDimensions()
    {
        return InputDimensions;
    }

    public int GetStride() => Stride;

    public List<YoloBoundingBox> PreprocessAndRun(Image<Rgb24> image)
    {
        using MemoryTensorOwner<float> memoryTensorOwner = _memoryAllocatorService.AllocateTensor<float>(_tensorShape, true);
        var pads = floatPool.Rent(1);
        var ratios = floatPool.Rent(1);
        image.NormalizeInput(memoryTensorOwner.Tensor, inputSize, ratios, pads, true);
        using var ortInput = memoryTensorOwner.Tensor.CreateOrtValue();

        var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), ortInput } };
        using var fromResult = _session.Run(_runOptions, inputs, OutputNames);
        float[] resultArrays = fromResult[0].Value.GetTensorDataAsSpan<float>().ToArray();

        YoloPrediction predictions = new YoloPrediction(resultArrays, CategoryReadOnlyCollection.ToArray(),
            [pads], [ratios], [[image.Height, image.Width]]);
        floatPool.Return(pads);
        floatPool.Return(ratios);
        return predictions.GetDetect();
    }

    public void WarmUp()
    {
        // using var inputOrtValue = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, InputDimensions.Select(x => (long)x).ToArray());
        // var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), inputOrtValue } };
        _session.RunWithBinding(_runOptions, OrtIoBinding);
        // using var fromResult = Session.Run(runOptions, inputs, OutputNames);
    }

    public void SetInput()
    {
    }

    public void SetOutput()
    {
    }

    public List<YoloBoundingBox> Predict(YoloFeeder tensorFeed)
    {
        var feed = tensorFeed.GetBatchTensor();
        var tensor = feed.tensor;
        long[] newDim = [tensor.Dimensions[0], tensor.Dimensions[1], tensor.Dimensions[2], tensor.Dimensions[3]];
        OutputDimensions[0] = newDim[0];
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, tensor.Buffer, newDim);
        var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), inputOrtValue } };
        using var fromResult = _session.Run(_runOptions, inputs, OutputNames);

        float[] resultArrays = fromResult[0].Value.GetTensorDataAsSpan<float>().ToArray();

        YoloPrediction predictions = new YoloPrediction(resultArrays, CategoryReadOnlyCollection.ToArray(), feed.dwdhs, feed.ratios, feed.imageShape);
        return predictions.GetDetect();
    }
}
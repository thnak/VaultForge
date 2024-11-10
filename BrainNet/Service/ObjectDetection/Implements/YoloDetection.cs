using BrainNet.Models.Setting;
using BrainNet.Service.ObjectDetection.Interfaces;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;
using BrainNet.Utils;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;

namespace BrainNet.Service.ObjectDetection.Implements;

public class YoloDetection : IYoloDetection
{
    private InferenceSession Session { get; set; } = null!;
    private IOptions<BrainNetSettingModel> Options { get; }
    private string[] InputNames { get; set; } = null!;
    private string[] OutputNames { get; set; } = null!;
    private int[] InputDimensions { get; set; } = [];
    public IReadOnlyCollection<string> CategoryReadOnlyCollection { get; set; } = [];

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
        Options = new OptionsWrapper<BrainNetSettingModel>(settings);
        InitializeSession(modelPath);
        InitCategory();
    }

    private void InitializeSession(string modelPath)
    {
        var sessionOption = InitSessionOption();
        Session = new InferenceSession(modelPath, sessionOption);
        InputNames = Session.GetInputNames();
        OutputNames = Session.GetOutputNames();
        InputDimensions = Session.InputMetadata.First().Value.Dimensions;
    }

    private SessionOptions InitSessionOption()
    {
        var sessionOptions = new SessionOptions();
        sessionOptions.RegisterOrtExtensions();
        sessionOptions.InitSessionOption();
        sessionOptions.InitExecutionProviderOptions(Options.Value.FaceEmbeddingSetting.DeviceIndex);
        return sessionOptions;
    }

    private void InitCategory()
    {
        var metadata = Session.ModelMetadata;
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
    }

    public void Dispose()
    {
        Session.Dispose();
    }

    public List<YoloBoundingBox> Predict(YoloFeeder tensorFeed)
    {
        var feed = tensorFeed.GetBatchTensor();
        var tensor = feed.tensor;
        long[] newDim = [tensor.Dimensions[0], tensor.Dimensions[1], tensor.Dimensions[2], tensor.Dimensions[3]];
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, tensor.Buffer, newDim);
        var inputs = new Dictionary<string, OrtValue> { { InputNames.First(), inputOrtValue } };

        using var fromResult = Session.Run(new RunOptions(), inputs, OutputNames);

        float[] resultArrays = fromResult[0].Value.GetTensorDataAsSpan<float>().ToArray();


        YoloPrediction predictions = new YoloPrediction(resultArrays, CategoryReadOnlyCollection.ToArray(), feed.dwdhs, feed.ratios, feed.imageShape);
        return predictions.GetDetect();
    }
}
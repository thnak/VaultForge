using BrainNet.Models.Setting;
using BrainNet.Service.FaceEmbedding.Interfaces;
using BrainNet.Service.FaceEmbedding.Utils;
using BrainNet.Utils;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BrainNet.Service.FaceEmbedding.Implements;

public class FaceEmbedding : IFaceEmbedding
{
    private InferenceSession Session { get; set; } = null!;
    private IOptions<BrainNetSettingModel> Options { get; }
    private string[] InputNames { get; set; } = null!;
    private string[] OutputNames { get; set; } = null!;
    private int[] InputDimensions { get; set; } = [];

    public FaceEmbedding(IOptions<BrainNetSettingModel> option)
    {
        Options = option;
        var modelPath = option.Value.FaceEmbeddingSetting.FaceEmbeddingPath;
        InitializeSession(modelPath);
    }

    public FaceEmbedding(string modelPath)
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
    }

    private void InitializeSession(string modelPath)
    {
        var sessionOption = InitSessionOption();
        Session = new InferenceSession(modelPath, sessionOption);
        InputNames = Session.GetInputNames();
        OutputNames = Session.GetOutputNames();
        InputDimensions = Session.InputMetadata.First().Value.Dimensions;
    }

    public float[] GetEmbeddingArray(Stream stream)
    {
        Image<Rgb24> image = Image.Load<Rgb24>(stream);
        return ProcessImageAsync(image);
    }

    public float[] GetEmbeddingArray(string imagePath)
    {
        Image<Rgb24> image = Image.Load<Rgb24>(imagePath);
        return ProcessImageAsync(image);
    }

    private float[] ProcessImageAsync(Image<Rgb24> image)
    {
        int[] tensorShape = [..InputDimensions];
        tensorShape[0] = 1;
        image.Mutate(x => x.Resize(tensorShape[2], tensorShape[3]));
        DenseTensor<float> processedImage = new(tensorShape);
        image.PreprocessImage(processedImage);
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, processedImage.Buffer, tensorShape.Select(x => (long)x).ToArray());

        using var outputs = Session.Run(new RunOptions(), InputNames, [inputOrtValue], OutputNames);
        return outputs.First().GetTensorDataAsSpan<float>().ToArray();
    }

    private SessionOptions InitSessionOption()
    {
        var sessionOptions = new SessionOptions();
        sessionOptions.InitSessionOption();
        sessionOptions.InitExecutionProviderOptions(Options.Value.FaceEmbeddingSetting.DeviceIndex);
        return sessionOptions;
    }


    public void Dispose()
    {
        Session.Dispose();
    }
}
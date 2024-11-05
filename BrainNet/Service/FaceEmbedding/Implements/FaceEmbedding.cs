using BrainNet.Models.Setting;
using BrainNet.Service.FaceEmbedding.Interfaces;
using BrainNet.Service.FaceEmbedding.Utils;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BrainNet.Service.FaceEmbedding.Implements;

public class FaceEmbedding : IFaceEmbedding
{
    private InferenceSession Session { get; set; }
    private IOptions<BrainNetSettingModel> Options { get; }
    private string[] InputNames { get; }
    private string[] OutputNames { get; }

    public FaceEmbedding(IOptions<BrainNetSettingModel> option)
    {
        Options = option;
        var modelPath = option.Value.FaceEmbeddingSetting.FaceEmbeddingPath;
        var sessionOption = InitSessionOption();
        Session = new InferenceSession(modelPath, sessionOption);
        InputNames = Session.GetInputNames();
        OutputNames = Session.GetOutputNames();
    }

    public async Task<float[]> GetEmbeddingArray(Stream stream)
    {
        Image<Rgb24> image = Image.Load<Rgb24>(stream);
        int[] tensorShape = [1, 3, Options.Value.FaceEmbeddingSetting.Height, Options.Value.FaceEmbeddingSetting.Width];
        image.Mutate(x => x.Resize(Options.Value.FaceEmbeddingSetting.Height, Options.Value.FaceEmbeddingSetting.Width));
        DenseTensor<float> processedImage = new(tensorShape);
        image.PreprocessImage(processedImage);
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, processedImage.Buffer, tensorShape.Select(x => (long)x).ToArray());

        OrtValue[] outputs = [];
        await Session.RunAsync(new RunOptions(), InputNames, [inputOrtValue], OutputNames, outputs);
        return outputs[0].GetTensorDataAsSpan<float>().ToArray();
    }

    private SessionOptions InitSessionOption()
    {
        var sessionOptions = new SessionOptions();
        sessionOptions.EnableMemoryPattern = true;
        sessionOptions.EnableCpuMemArena = true;
        sessionOptions.EnableProfiling = false;
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
        sessionOptions.InitExecutionProviderOptions(Options.Value.FaceEmbeddingSetting.DeviceIndex);
        return sessionOptions;
    }


    public void Dispose()
    {
        Session.Dispose();
    }
}
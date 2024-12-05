using Microsoft.ML.OnnxRuntime;

namespace BrainNet.Utils;

public static class SessionOptionExtension
{
    public static void InitSessionOption(this SessionOptions sessionOptions)
    {
        sessionOptions.EnableMemoryPattern = true;
        sessionOptions.EnableCpuMemArena = true;
        sessionOptions.EnableProfiling = false;
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
    }

    public static string InitExecutionProviderOptions(this SessionOptions options, int deviceId)
    {
        var providers = OrtEnv.Instance().GetAvailableProviders();
        var availableProvider = providers[0];
        switch (availableProvider)
        {
            case "CUDAExecutionProvider":
            {
                OrtCUDAProviderOptions providerOptions = new OrtCUDAProviderOptions();
                var providerOptionsDict = new Dictionary<string, string>
                {
                    ["cudnn_conv_use_max_workspace"] = "1",
                    ["device_id"] = $"{deviceId}"
                };
                providerOptions.UpdateOptions(providerOptionsDict);
                options.AppendExecutionProvider_CUDA(providerOptions);
                break;
            }
            case "TensorrtExecutionProvider":
            {
                OrtTensorRTProviderOptions provider = new OrtTensorRTProviderOptions();
                var providerOptionsDict = new Dictionary<string, string>
                {
                    ["cudnn_conv_use_max_workspace"] = "1",
                    ["device_id"] = $"{deviceId}",
                    ["ORT_TENSORRT_FP16_ENABLE"] = "true",
                    ["ORT_TENSORRT_LAYER_NORM_FP32_FALLBACK"] = "true",
                    ["ORT_TENSORRT_ENGINE_CACHE_ENABLE"] = "true",
                };
                provider.UpdateOptions(providerOptionsDict);
                options.AppendExecutionProvider_Tensorrt(provider);
                break;
            }
            case "DNNLExecutionProvider":
            {
                options.AppendExecutionProvider_OpenVINO($"{deviceId}");
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
                break;
            }
            case "OpenVINOExecutionProvider":
            {
                break;
            }
            case "DmlExecutionProvider":
            {
                options.EnableMemoryPattern = false;
                options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                options.AppendExecutionProvider_DML(deviceId);
                break;
            }
            case "ROCMExecutionProvider":
            {
                OrtROCMProviderOptions provider = new();
                var providerOptionsDict = new Dictionary<string, string>
                {
                    ["device_id"] = $"{deviceId}",
                    ["cudnn_conv_use_max_workspace"] = "1"
                };
                provider.UpdateOptions(providerOptionsDict);
                options.AppendExecutionProvider_ROCm(provider);
                break;
            }
        }
        return availableProvider;
    }

    public static string[] GetInputNames(this InferenceSession session)
    {
        var inputNames = session.InputMetadata.Keys.ToArray();
        return inputNames;
    }

    public static string[] GetOutputNames(this InferenceSession session)
    {
        var outputNames = session.OutputMetadata.Keys.ToArray();
        return outputNames;
    }
}
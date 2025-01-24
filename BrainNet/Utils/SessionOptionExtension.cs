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
        sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING;
        sessionOptions.OptimizedModelFilePath = "optimized_model.onnx";
    }

    public static SessionOptions MakeSessionOption()
    {
        var providers = OrtEnv.Instance().GetAvailableProviders().Where(x => x != "TensorrtExecutionProvider");
        var availableProvider = providers.First();
        switch (availableProvider)
        {
            case "CUDAExecutionProvider":
                return SessionOptions.MakeSessionOptionWithCudaProvider();
            
        }
        return new SessionOptions();
    }

    public static string InitExecutionProviderOptions(this SessionOptions options, int deviceId)
    {
        var providers = OrtEnv.Instance().GetAvailableProviders();
        var availableProvider = providers.First();
        switch (availableProvider)
        {
            case "CUDAExecutionProvider":
            {
                OrtCUDAProviderOptions providerOptions = new OrtCUDAProviderOptions();
                var providerOptionsDict = new Dictionary<string, string>
                {
                    ["device_id"] = $"{deviceId}",
                    ["cudnn_conv_algo_search"] = "EXHAUSTIVE"
                };
                options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                providerOptions.UpdateOptions(providerOptionsDict);
                options.AppendExecutionProvider_CUDA(providerOptions);
                break;
            }
            case "TensorrtExecutionProvider":
            {
                OrtTensorRTProviderOptions provider = new OrtTensorRTProviderOptions();
                var providerOptionsDict = new Dictionary<string, string>
                {
                    ["device_id"] = $"{deviceId}",
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
                options.EnableCpuMemArena = false;
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
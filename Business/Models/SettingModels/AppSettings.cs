using System.Reflection;
using BrainNet.Models.Setting;
using BusinessModels.System.FileSystem;

namespace Business.Models.SettingModels;

public class AppSettings
{
    public DbSettingModel DbSetting { get; set; } = new();
    public BackgroundQueue BackgroundQueue { get; set; } = new();
    public ThumbnailSetting ThumbnailSetting { get; set; } = new();
    public Storage Storage { get; set; } = new();
    public VideoTransCode VideoTransCode { get; set; } = new();
    public OllamaConfig OllamaConfig { get; set; } = new();
    public OnnxConfig OnnxConfig { get; set; } = new();
    public IoTCircuitBreaker IoTCircuitBreaker { get; set; } = new();
    public IoTRequestQueueConfig IoTRequestQueueConfig { get; set; } = new();
    public AppCertificate AppCertificate { get; set; } = new();

    public Authenticate Authenticate { get; set; } = new();
    
    public BrainNetSettingModel BrainNetSettingModel { get; set; } = new();
}

public class BackgroundQueue
{
    public int SequenceQueueSize { get; set; } = 5 * 1024 * 1024;
    public int ParallelQueueSize { get; set; } = 5 * 1024 * 1024;
    public int MaxParallelThreads { get; set; } = Math.Max(Environment.ProcessorCount - 4, 1);
}

public class Storage
{
    public string[] Disks { get; set; } = [];
    public RaidType DefaultRaidType { get; set; } = RaidType.Raid5;
    public string[] FolderWatchList { get; set; } = [];
    public int BufferSize { get; set; } = 4096 * 2;
    public int StripSize { get; set; } = 4096;
}

public class ThumbnailSetting
{
    public int ImageThumbnailSize { get; set; } = 480;
}

public class VideoTransCode
{
    public string WorkingDirectory { get; set; } = "/home/thnak";
    public string VideoEncoder { get; set; } = "h264";
}

public class DbSettingModel
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MaxConnectionPoolSize { get; set; } = 200;
}

public class OllamaConfig
{
    public string ConnectionString { get; set; } = "http:thnakdevserver.ddns.net:11434/";
    public string TextEmbeddingModel { get; set; } = "nomic-embed-text";
    public int WikiVectorSize { get; set; } = 512;
    public string TextGeneratorModel { get; set; } = "llama3.2";
    public string Image2TextModel { get; set; } = "minicpm-v";
}

public class OnnxConfig
{
    public string FaceDetectionPath { get; set; } = string.Empty;
    public string WaterMeterWeightPath { get; set; } = string.Empty;
    public FaceEmbeddingModel FaceEmbeddingModel { get; set; } = new();
}

public class FaceEmbeddingModel
{
    public string ModelPath { get; set; } = string.Empty;
    public int VectorSize { get; set; } = 4096;
    public string DistantFunc { get; set; } = "EuclideanDistance";
}

public class IoTCircuitBreaker
{
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;
    public int DurationOfBreakInSecond { get; set; } = 30;
}

public class IoTRequestQueueConfig
{
    public int MaxQueueSize { get; set; } = 100_000;
    public int TimePeriodInSecond { get; set; } = 5;
}

public class AppCertificate
{
    public string FilePath { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class Authenticate()
{
    public string Pepper { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public static class AppSettingsConverter
{
    public static Dictionary<string, Dictionary<string, string>> ConvertToDictionary(this AppSettings appSettings)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        foreach (var property in appSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var groupName = property.Name;
            var groupValue = property.GetValue(appSettings);

            if (groupValue != null)
            {
                var groupDictionary = new Dictionary<string, string>();

                foreach (var subProperty in groupValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var key = subProperty.Name;
                    var value = subProperty.GetValue(groupValue) ?? null;
                    if (value is Array array)
                    {
                        groupDictionary[key] = string.Join(", ", array.Cast<object>());
                    }
                    else
                    {
                        groupDictionary[key] = value?.ToString() ?? "null";
                    }
                }

                result[groupName] = groupDictionary;
            }
        }

        return result;
    }
}
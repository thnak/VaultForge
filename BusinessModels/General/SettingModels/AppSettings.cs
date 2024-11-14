using BusinessModels.System.FileSystem;

namespace BusinessModels.General.SettingModels;

public class AppSettings
{
    public DbSettingModel DbSetting { get; set; } = new();
    public BackgroundQueue BackgroundQueue { get; set; } = new();
    public ThumbnailSetting ThumbnailSetting { get; set; } = new();
    public Storage Storage { get; set; } = new();
    public VideoTransCode VideoTransCode { get; set; } = new();
    public OllamaConfig OllamaConfig { get; set; } = new();
    public IoTCircuitBreaker IoTCircuitBreaker { get; set; } = new();
    public IoTRequestQueueConfig IoTRequestQueueConfig { get; set; } = new();
    public AppCertificate AppCertificate { get; set; } = new();
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
    public string ConnectionString { get; set; } = "http://localhost:11434/";
    public string TextEmbeddingModel { get; set; } = "nomic-embed-text";
    public string TextGeneratorModel { get; set; } = "llama3.2";
    public string Image2TextModel { get; set; } = "minicpm-v";
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
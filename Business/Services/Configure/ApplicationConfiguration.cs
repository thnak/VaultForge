using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Options;

namespace Business.Services.Configure;

public class ApplicationConfiguration
{
    private AppSettings Configs { get; set; } = new();

    public ApplicationConfiguration(IOptions<AppSettings> appSettings)
    {
        InitOllamaConfig(appSettings);

        InitStorageConfig(appSettings);

        InitIotQueueConfig(appSettings);

        InitIotCircuitConfig(appSettings);

        InitBackgroundQueueConfig(appSettings);

        InitImageThumbnailConfig(appSettings);

        InitDatabaseConfig(appSettings);

        InitCertificateConfig(appSettings);

        InitVideoEncodeCofig(appSettings);

        DisplayGroupedConfigurations(InitLogoAsciiArt(), Configs.ConvertToDictionary());
    }

    private void InitVideoEncodeCofig(IOptions<AppSettings> appSettings)
    {
        Configs.VideoTransCode.WorkingDirectory = GetEnvironmentVariable("VideoTransCodeWorkingDirectory", appSettings.Value.VideoTransCode.WorkingDirectory);
        Configs.VideoTransCode.VideoEncoder = GetEnvironmentVariable("VideoTransCodeVideoEncoder", appSettings.Value.VideoTransCode.VideoEncoder);
    }

    private void InitCertificateConfig(IOptions<AppSettings> appSettings)
    {
        Configs.AppCertificate.FilePath = GetEnvironmentVariable("AppCertificateFilePath", appSettings.Value.AppCertificate.FilePath);
        Configs.AppCertificate.Password = GetEnvironmentVariable("AppCertificatePassword", appSettings.Value.AppCertificate.Password);
    }

    private void InitImageThumbnailConfig(IOptions<AppSettings> appSettings)
    {
        Configs.ThumbnailSetting.ImageThumbnailSize = GetIntEnvironmentVariable("ThumbnailSettingImageThumbnailSize", appSettings.Value.ThumbnailSetting.ImageThumbnailSize);
    }

    private void InitIotCircuitConfig(IOptions<AppSettings> appSettings)
    {
        Configs.IoTCircuitBreaker.ExceptionsAllowedBeforeBreaking = GetIntEnvironmentVariable("IoTCircuitBreakerExceptionsAllowedBeforeBreaking", appSettings.Value.IoTCircuitBreaker.ExceptionsAllowedBeforeBreaking);
        Configs.IoTCircuitBreaker.DurationOfBreakInSecond = GetIntEnvironmentVariable("IoTCircuitBreakerDurationOfBreakInSecond", appSettings.Value.IoTCircuitBreaker.DurationOfBreakInSecond);
    }

    private void InitBackgroundQueueConfig(IOptions<AppSettings> appSettings)
    {
        Configs.BackgroundQueue.ParallelQueueSize = GetIntEnvironmentVariable("BackgroundQueueParallelQueueSize", appSettings.Value.BackgroundQueue.ParallelQueueSize);
        Configs.BackgroundQueue.SequenceQueueSize = GetIntEnvironmentVariable("BackgroundQueueSequenceQueueSize", appSettings.Value.BackgroundQueue.SequenceQueueSize);
        Configs.BackgroundQueue.MaxParallelThreads = GetIntEnvironmentVariable("BackgroundQueueMaxParallelThreads", appSettings.Value.BackgroundQueue.MaxParallelThreads);
    }

    private void InitDatabaseConfig(IOptions<AppSettings> appSettings)
    {
        Configs.DbSetting.ConnectionString = GetEnvironmentVariable("DbSettingConnectionString", appSettings.Value.DbSetting.ConnectionString);
        Configs.DbSetting.DatabaseName = GetEnvironmentVariable("DbSettingDatabaseName", appSettings.Value.DbSetting.DatabaseName);
        Configs.DbSetting.Password = GetEnvironmentVariable("DbSettingPassword", appSettings.Value.DbSetting.Password);
        Configs.DbSetting.UserName = GetEnvironmentVariable("DbSettingUserName", appSettings.Value.DbSetting.UserName);
        Configs.DbSetting.Port = GetIntEnvironmentVariable("DbSettingPort", appSettings.Value.DbSetting.Port);
        Configs.DbSetting.MaxConnectionPoolSize = GetIntEnvironmentVariable("DbSettingMaxConnectionPoolSize", appSettings.Value.DbSetting.MaxConnectionPoolSize);
    }

    private void InitIotQueueConfig(IOptions<AppSettings> appSettings)
    {
        Configs.IoTRequestQueueConfig.MaxQueueSize = GetIntEnvironmentVariable("IoTRequestQueueConfigMaxQueueSize", appSettings.Value.IoTRequestQueueConfig.MaxQueueSize);
        Configs.IoTRequestQueueConfig.TimePeriodInSecond = GetIntEnvironmentVariable("IoTRequestQueueConfigTimePeriodInSecond", appSettings.Value.IoTRequestQueueConfig.TimePeriodInSecond);
    }

    private void InitStorageConfig(IOptions<AppSettings> appSettings)
    {
        Configs.Storage.Disks = GetEnvironmentVariables("StorageDisks", appSettings.Value.Storage.Disks);
        Configs.Storage.DefaultRaidType = GetEnumEnvironmentVariable("StorageDefaultRaidType", appSettings.Value.Storage.DefaultRaidType);
        Configs.Storage.FolderWatchList = GetEnvironmentVariables("StorageFolderWatchList", appSettings.Value.Storage.FolderWatchList);
        Configs.Storage.BufferSize = GetIntEnvironmentVariable("StorageBufferSize", appSettings.Value.Storage.BufferSize);
        Configs.Storage.StripSize = GetIntEnvironmentVariable("StorageStripSize", appSettings.Value.Storage.StripSize);
    }

    private void InitOllamaConfig(IOptions<AppSettings> appSettings)
    {
        Configs.OllamaConfig.ConnectionString = GetEnvironmentVariable("OllamaConfigConnectionString", appSettings.Value.OllamaConfig.ConnectionString);
        Configs.OllamaConfig.Image2TextModel = GetEnvironmentVariable("OllamaConfigImage2TextModel", appSettings.Value.OllamaConfig.Image2TextModel);
        Configs.OllamaConfig.TextEmbeddingModel = GetEnvironmentVariable("OllamaConfigTextEmbeddingModel", appSettings.Value.OllamaConfig.TextEmbeddingModel);
        Configs.OllamaConfig.TextGeneratorModel = GetEnvironmentVariable("OllamaConfigTextGeneratorModel", appSettings.Value.OllamaConfig.TextGeneratorModel);
    }

    public OllamaConfig GetOllamaConfig => Configs.OllamaConfig;
    public Storage GetStorage => Configs.Storage;
    public IoTRequestQueueConfig GetIoTRequestQueueConfig => Configs.IoTRequestQueueConfig;
    public IoTCircuitBreaker GetIoTCircuitBreaker => Configs.IoTCircuitBreaker;
    public ThumbnailSetting GetThumbnailSetting => Configs.ThumbnailSetting;
    public DbSettingModel GetDbSetting => Configs.DbSetting;
    public BackgroundQueue GetBackgroundQueue => Configs.BackgroundQueue;
    public AppCertificate GetAppCertificate => Configs.AppCertificate;
    public VideoTransCode GetVideoTransCode => Configs.VideoTransCode;

    private string GetEnvironmentVariable(string key, string defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        return defaultValue;
    }

    private string[] GetEnvironmentVariables(string key, string[] defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            var array = envValue.Split(",");
            return array.Any() ? array : defaultValue;
        }

        return defaultValue;
    }

    private T GetEnumEnvironmentVariable<T>(string key, T defaultValue) where T : struct
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            var enumValue = Enum.Parse<T>(envValue);
            return enumValue;
        }

        return defaultValue;
    }

    private int GetIntEnvironmentVariable(string key, int defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            var intValue = int.Parse(envValue);
            return intValue;
        }

        return defaultValue;
    }

    private string InitLogoAsciiArt()
    {
        return @"
__| |__________________________________________________________________________________| |__
__   __________________________________________________________________________________   __
  | |                                                                                  | |  
  | |██╗   ██╗ █████╗ ██╗   ██╗██╗  ████████╗███████╗ ██████╗ ██████╗  ██████╗ ███████╗| |  
  | |██║   ██║██╔══██╗██║   ██║██║  ╚══██╔══╝██╔════╝██╔═══██╗██╔══██╗██╔════╝ ██╔════╝| |  
  | |██║   ██║███████║██║   ██║██║     ██║   █████╗  ██║   ██║██████╔╝██║  ███╗█████╗  | |  
  | |╚██╗ ██╔╝██╔══██║██║   ██║██║     ██║   ██╔══╝  ██║   ██║██╔══██╗██║   ██║██╔══╝  | |  
  | | ╚████╔╝ ██║  ██║╚██████╔╝███████╗██║   ██║     ╚██████╔╝██║  ██║╚██████╔╝███████╗| |  
  | |  ╚═══╝  ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝   ╚═╝      ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝| |  
__| |__________________________________________________________________________________| |__
__   __________________________________________________________________________________   __
  | |                                                                                  | |  
        ";
    }

    public static void DisplayGroupedConfigurations(string appNameAscii, Dictionary<string, Dictionary<string, string>> groupedConfigurations)
    {
        // Display the ASCII art logo
        Console.WriteLine(appNameAscii);
        Console.WriteLine(new string('=', 100));
        Console.WriteLine(@"VaultForge Configuration");
        Console.WriteLine(new string('=', 100));

        // Loop through each configuration group
        foreach (var group in groupedConfigurations)
        {
            Console.WriteLine();
            Console.WriteLine($@"[ {group.Key.ToUpper()} ]"); // Group title
            Console.WriteLine(new string('-', 100)); // Separator

            foreach (var config in group.Value)
            {
                Console.WriteLine($@"{config.Key,-30} : {config.Value}");
            }

            Console.WriteLine(new string('-', 100)); // Footer separator
        }

        Console.WriteLine();
        Console.WriteLine(@"Application started successfully!");
    }
}
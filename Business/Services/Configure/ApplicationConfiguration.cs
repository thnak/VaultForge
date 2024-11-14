using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Options;

namespace Business.Services.Configure;

public class ApplicationConfiguration
{
    private AppSettings Configs { get; set; } = new();

    public ApplicationConfiguration(IOptions<AppSettings> appSettings)
    {
        Configs.OllamaConfig.ConnectionString = GetEnvironmentVariable(nameof(appSettings.Value.OllamaConfig.ConnectionString), appSettings.Value.OllamaConfig.ConnectionString);
        Configs.OllamaConfig.Image2TextModel = GetEnvironmentVariable(nameof(appSettings.Value.OllamaConfig.Image2TextModel), appSettings.Value.OllamaConfig.Image2TextModel);
        Configs.OllamaConfig.TextEmbeddingModel = GetEnvironmentVariable(nameof(appSettings.Value.OllamaConfig.TextEmbeddingModel), appSettings.Value.OllamaConfig.TextEmbeddingModel);
        Configs.OllamaConfig.TextGeneratorModel = GetEnvironmentVariable(nameof(appSettings.Value.OllamaConfig.TextGeneratorModel), appSettings.Value.OllamaConfig.TextGeneratorModel);

        Configs.Storage.Disks = GetEnvironmentVariables("StorageDisks", appSettings.Value.Storage.Disks);
        Configs.Storage.DefaultRaidType = GetEnumEnvironmentVariable("StorageDefaultRaidType", appSettings.Value.Storage.DefaultRaidType);
        Configs.Storage.FolderWatchList = GetEnvironmentVariables("StorageFolderWatchList", appSettings.Value.Storage.FolderWatchList);
        Configs.Storage.BufferSize = GetIntEnvironmentVariable("StorageBufferSize", appSettings.Value.Storage.BufferSize);
        Configs.Storage.StripSize = GetIntEnvironmentVariable("StorageStripSize", appSettings.Value.Storage.StripSize);

        Configs.IoTRequestQueueConfig.MaxQueueSize = GetIntEnvironmentVariable("IoTRequestQueueConfigMaxQueueSize", appSettings.Value.IoTRequestQueueConfig.MaxQueueSize);
        Configs.IoTRequestQueueConfig.TimePeriodInSecond = GetIntEnvironmentVariable("IoTRequestQueueConfigTimePeriodInSecond", appSettings.Value.IoTRequestQueueConfig.TimePeriodInSecond);

        Configs.IoTCircuitBreaker.ExceptionsAllowedBeforeBreaking = GetIntEnvironmentVariable("IoTCircuitBreakerExceptionsAllowedBeforeBreaking", appSettings.Value.IoTCircuitBreaker.ExceptionsAllowedBeforeBreaking);
        Configs.IoTCircuitBreaker.DurationOfBreakInSecond = GetIntEnvironmentVariable("IoTCircuitBreaker.DurationOfBreakInSecond", appSettings.Value.IoTCircuitBreaker.DurationOfBreakInSecond);

        Configs.BackgroundQueue.ParallelQueueSize = GetIntEnvironmentVariable("BackgroundQueueParallelQueueSize", appSettings.Value.BackgroundQueue.ParallelQueueSize);
        Configs.BackgroundQueue.SequenceQueueSize = GetIntEnvironmentVariable("BackgroundQueueSequenceQueueSize", appSettings.Value.BackgroundQueue.SequenceQueueSize);
        Configs.BackgroundQueue.MaxParallelThreads = GetIntEnvironmentVariable("BackgroundQueueMaxParallelThreads", appSettings.Value.BackgroundQueue.MaxParallelThreads);

        Configs.ThumbnailSetting.ImageThumbnailSize = GetIntEnvironmentVariable("ThumbnailSetting.ImageThumbnailSize", appSettings.Value.ThumbnailSetting.ImageThumbnailSize);

        Configs.DbSetting.ConnectionString = GetEnvironmentVariable("DbSettingConnectionString", appSettings.Value.DbSetting.ConnectionString);
        Configs.DbSetting.DatabaseName = GetEnvironmentVariable("DbSettingDatabaseName", appSettings.Value.DbSetting.DatabaseName);
        Configs.DbSetting.Password = GetEnvironmentVariable("DbSettingPassword", appSettings.Value.DbSetting.Password);
        Configs.DbSetting.UserName = GetEnvironmentVariable("DbSettingUserName", appSettings.Value.DbSetting.UserName);
        Configs.DbSetting.Port = GetIntEnvironmentVariable("DbSettingPort", appSettings.Value.DbSetting.Port);
        Configs.DbSetting.MaxConnectionPoolSize = GetIntEnvironmentVariable("DbSettingMaxConnectionPoolSize", appSettings.Value.DbSetting.MaxConnectionPoolSize);

        Configs.AppCertificate.FilePath = GetEnvironmentVariable("AppCertificateFilePath", appSettings.Value.AppCertificate.FilePath);
        Configs.AppCertificate.Password = GetEnvironmentVariable("AppCertificatePassword", appSettings.Value.AppCertificate.Password);
    }

    public OllamaConfig GetOllamaConfig => Configs.OllamaConfig;
    public Storage GetStorage => Configs.Storage;
    public IoTRequestQueueConfig GetIoTRequestQueueConfig => Configs.IoTRequestQueueConfig;
    public IoTCircuitBreaker GetIoTCircuitBreaker => Configs.IoTCircuitBreaker;
    public ThumbnailSetting GetThumbnailSetting => Configs.ThumbnailSetting;
    public DbSettingModel GetDbSetting => Configs.DbSetting;
    public BackgroundQueue GetBackgroundQueue => Configs.BackgroundQueue;

    public AppCertificate GetAppCertificate => Configs.AppCertificate;

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
}
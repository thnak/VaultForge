namespace BusinessModels.General.SettingModels;

public class AppSettings
{
    public string FileFolder { get; set; } = string.Empty;
    public string[] FileFolders { get; set; } = [];
    public int StripeSize { get; set; } = 4096;
    public int ReadWriteBufferSize { get; set; } = 10 * 1024 * 1024;
    public string[] FolderWatchList { get; set; } = [];
    public BackgroundQueue BackgroundQueue { get; set; } = new();
    public ThumbnailSetting ThumbnailSetting { get; set; } = new();
    public string TransCodeConverterScriptDir { get; set; } = "/home/thnak";
}

public class BackgroundQueue
{
    public int SequenceQueueSize { get; set; } = 5 * 1024 * 1024;
    public int ParallelQueueSize { get; set; } = 5 * 1024 * 1024;
    public int MaxParallelThreads { get; set; } = Math.Max(Environment.ProcessorCount - 4, 1);
}

public class ThumbnailSetting
{
    public int ImageThumbnailSize { get; set; } = 480;
}
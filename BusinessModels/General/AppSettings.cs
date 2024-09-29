namespace BusinessModels.General;

public class AppSettings
{
    public string FileFolder { get; set; } = string.Empty;
    public string[] FileFolders { get; set; } = [];
    public int StripeSize { get; set; } = 4096;
    public int ReadWriteBufferSize { get; set; } = 10 * 1024 * 1024;
}
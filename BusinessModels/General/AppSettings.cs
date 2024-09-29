namespace BusinessModels.General;

public class AppSettings
{
    public string FileFolder { get; set; } = string.Empty;
    public string[] FileFolders { get; set; } = [];
    public int StripeSize { get; set; } = 1024;
}
namespace BusinessModels.General.SettingModels;

public class DbSettingModel
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
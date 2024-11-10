using BusinessModels.Base;

namespace BusinessModels.System.VersionControlModel;

public class VersionRegisterModel : BaseModelEntry
{
    public string Hash { get; set; } = string.Empty;

    public DateTime Time { get; set; }
}
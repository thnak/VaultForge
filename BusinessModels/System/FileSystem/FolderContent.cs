using BusinessModels.General.EnumModel;
using MessagePack;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FolderContent
{
    [Key(0)] public string Id { get; set; } = string.Empty;

    [Key(1)] public FolderContentType Type { get; set; }
}


namespace BusinessModels.General.EnumModel;

public enum FileStatus
{
    File,
    HiddenFile,
    DeletedFile,
    CorruptedFile,
    MissingFile,
}

public enum FileClassify
{
    Normal,
    ThumbnailFile,
    ThumbnailWebpFile,
    FounderFile,
    M3U8File,
    M3U8FileSegment
}
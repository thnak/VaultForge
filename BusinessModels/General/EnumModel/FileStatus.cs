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
    /// <summary>
    /// xem trước
    /// </summary>
    ThumbnailFile,
    /// <summary>
    /// tối ưu để xem trên web
    /// </summary>
    ThumbnailWebpFile,
    M3U8File,
    M3U8FileSegment,
    IotImageFile
}
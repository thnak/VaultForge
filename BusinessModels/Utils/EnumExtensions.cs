using BusinessModels.General.EnumModel;

namespace BusinessModels.Utils;

public static class EnumExtensions
{
    public static FileContentType MapFileContentType(this FolderContentType value)
    {
        switch (value)
        {
            case FolderContentType.Folder:
                return FileContentType.File;
            case FolderContentType.File:
                return FileContentType.File;
            case FolderContentType.HiddenFile:
                return FileContentType.HiddenFile;
            case FolderContentType.HiddenFolder:
                return FileContentType.HiddenFile;
            case FolderContentType.DeletedFile:
                return FileContentType.DeletedFile;
            case FolderContentType.DeletedFolder:
                return FileContentType.DeletedFile;
        }

        return FileContentType.HiddenFile;
    }
}
using BusinessModels.General.EnumModel;

namespace BusinessModels.Utils;

public static class EnumExtensions
{
    public static FileStatus MapFileContentType(this FolderContentType value)
    {
        switch (value)
        {
            case FolderContentType.Folder:
                return FileStatus.File;
            case FolderContentType.File:
                return FileStatus.File;
            case FolderContentType.HiddenFile:
                return FileStatus.HiddenFile;
            case FolderContentType.HiddenFolder:
                return FileStatus.HiddenFile;
            case FolderContentType.DeletedFile:
                return FileStatus.DeletedFile;
            case FolderContentType.DeletedFolder:
                return FileStatus.DeletedFile;
        }

        return FileStatus.HiddenFile;
    }
}
using BusinessModels.General.EnumModel;

namespace WebApp.Client.Utils;

public static class TypeClassList
{
    public static string ItemOpacityClass(this string self, FileContentType fileContentType)
    {
        var opacity = string.Empty;
        switch (fileContentType)
        {
            case FileContentType.HiddenFile:
            {
                opacity = "opacity-5";
                break;
            }
        }

        return self + $" {opacity}";
    }

    public static string ItemOpacityClass(this string self, FolderContentType fileContentType)
    {
        var opacity = string.Empty;
        switch (fileContentType)
        {
            case FolderContentType.HiddenFolder:
            {
                opacity = "opacity-5";
                break;
            }
        }

        return self + $" {opacity}";
    }
}
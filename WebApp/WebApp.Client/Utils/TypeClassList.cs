using Blazored.Toast.Configuration;
using BusinessModels.General.EnumModel;

namespace WebApp.Client.Utils;

public static class TypeClassList
{
    public static string ItemOpacityClass(this string self, FileContentType fileContentType, bool showHidden = false)
    {
        switch (fileContentType)
        {
            case FileContentType.HiddenFile:
            {
                var opacity = showHidden ? "opacity-5" : "d-none";
                return self + $" {opacity}";
            }
            default:
                return self;
        }
    }

    public static string ItemOpacityClass(this string self, FolderContentType fileContentType, bool showHidden = false)
    {
        switch (fileContentType)
        {
            case FolderContentType.HiddenFolder:
            {
                var opacity = showHidden ? "opacity-5" : "d-none";
                return self + $" {opacity}";
            }
            default:
                return self;
        }
    }

    public static void ToastDefaultSetting(ToastSettings toastSettings)
    {
        toastSettings.AdditionalClasses = "toast-move-right-2-left";
    }
}
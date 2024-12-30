using BusinessModels.General.Results;
using BusinessModels.Resources;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;

namespace WebApp.Client.Utils;

public static class DialogServiceExtensions
{
    private static readonly DialogOptions ConfirmDialogOptions = new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true
    };

    /// <summary>
    /// Open confirm dialog
    /// return true if confirmed
    /// </summary>
    /// <param name="dialogService"></param>
    /// <param name="title"></param>
    /// <param name="titleIcon"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static async Task<bool> OpenConfirmDialogAsync(this IDialogService dialogService, string title, string titleIcon, string message)
    {
        var param = new DialogParameters<ConfirmDialog>()
        {
            {
                x => x.DataModel, new DialogConfirmDataModel()
                {
                    TitleIcon = titleIcon,
                    Message = message,
                }
            }
        };
        var dialog = await dialogService.ShowAsync<ConfirmDialog>(title, param, ConfirmDialogOptions);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            return true;
        }

        return false;
    }

    public static async Task<bool> OpenConfirmDialogAsync(this IDialogService dialogService, string title, DialogConfirmDataModel dataModel)
    {
        var param = new DialogParameters<ConfirmDialog>()
        {
            { x => x.DataModel, dataModel }
        };
        var dialog = await dialogService.ShowAsync<ConfirmDialog>(title, param, ConfirmDialogOptions);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Open a dialog with text field inside
    /// return empty when canceled
    /// </summary>
    /// <param name="dialogService"></param>
    /// <param name="title"></param>
    /// <param name="fieldName"></param>
    /// <param name="oldValue"></param>
    /// <param name="icon"></param>
    /// <param name="titleIcoColor"></param>
    /// <returns></returns>
    public static async Task<Result<string>> OpenTextFieldDialog(this IDialogService dialogService, string title, string fieldName,
        string oldValue, string icon = "", Color titleIcoColor = Color.Default)
    {
        var dialogParam = new DialogParameters<ConfirmWithFieldDialog>
        {
            { x => x.FieldName, fieldName },
            { x => x.OldValueField, oldValue },
            { x => x.TitleIcon, icon },
            { x => x.TitleIconColor, titleIcoColor }
        };
        var dialog = await dialogService.ShowAsync<ConfirmWithFieldDialog>(title, dialogParam, ConfirmDialogOptions);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: string newName })
        {
            return Result<string>.Success(newName);
        }

        return Result<string>.Canceled(AppLang.Cancel);
    }
}
using Business.Business.Interfaces.FileSystem;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using FluentValidation;

namespace Business.Validator.Folder;

public class FolderInfoModelFluentValidator : AbstractValidator<FolderInfoModel>
{
    private string UserName { get; set; }
    private IFolderSystemBusinessLayer FolderSystemBusinessLayer { get; set; }

    public FolderInfoModelFluentValidator(IFolderSystemBusinessLayer folderSystemBusinessLayer, string userName)
    {
        UserName = userName;
        FolderSystemBusinessLayer = folderSystemBusinessLayer;
        RuleFor(x => x.RelativePath).MustAsync(Predicate).WithMessage(AppLang.Folder_already_exists);
        RuleFor(x => x.Username).NotEmpty().WithMessage(AppLang.User_information_is_required);
    }

    private Task<bool> Predicate(string arg1, CancellationToken arg2)
    {
        return Task.FromResult(FolderSystemBusinessLayer.Get(UserName, arg1) == null);
    }

    public Func<FolderInfoModel, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<FolderInfoModel>.CreateWithOptions(model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return [];
        return result.Errors.Select(e => e.ErrorMessage);
    };
}
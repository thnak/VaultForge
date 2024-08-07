using Business.Business.Interfaces.FileSystem;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Validator;
using FluentValidation;

namespace Business.Validator.Folder;

public class FolderInfoModelFluentValidator : ExtendFluentValidator<FolderInfoModel>
{
    public FolderInfoModelFluentValidator(IFolderSystemBusinessLayer folderSystemBusinessLayer, string userName)
    {
        UserName = userName;
        FolderSystemBusinessLayer = folderSystemBusinessLayer;
        RuleFor(x => x.RelativePath).MustAsync(Predicate).WithMessage(AppLang.Folder_already_exists);
        RuleFor(x => x.Username).NotEmpty().WithMessage(AppLang.User_information_is_required);
    }

    private string UserName { get; }
    private IFolderSystemBusinessLayer FolderSystemBusinessLayer { get; }

    private Task<bool> Predicate(string arg1, CancellationToken arg2)
    {
        return Task.FromResult(FolderSystemBusinessLayer.Get(UserName, arg1) == null);
    }
}
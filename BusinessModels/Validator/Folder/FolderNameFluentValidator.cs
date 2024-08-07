using BusinessModels.Resources;
using BusinessModels.Utils;
using FluentValidation;

namespace BusinessModels.Validator.Folder;

public class FolderNameFluentValidator : ExtendFluentValidator<string>
{
    public FolderNameFluentValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ValidateSystemPathName()).WithMessage(x =>
            {
                x.ValidateSystemPathName(out var c);
                return string.Format(AppLang.Folder_name_contains_invalid_character__x, c);
            })
            .NotEmpty().WithMessage(AppLang.ThisFieldIsRequired);
    }
}
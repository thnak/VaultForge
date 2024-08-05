using BusinessModels.Resources;
using BusinessModels.Utils;
using FluentValidation;

namespace BusinessModels.Validator.Folder;

public class FolderNameFluentValidator : AbstractValidator<string>
{
    public FolderNameFluentValidator()
    {
        RuleFor(x => x).Must(x => x.ValidateSystemPathName()).WithMessage(x =>
        {
            x.ValidateSystemPathName(out char? c);
            return string.Format(AppLang.Folder_name_contains_invalid_character__x, c);
        });
    }

    public Func<string, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<string>.CreateWithOptions(model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    };
}
using FluentValidation;

namespace BusinessModels.Validator;

/// <summary>
/// Mở rộng tụi Validator cho client
/// </summary>
/// <typeparam name="T"></typeparam>
public class ExtendFluentValidator<T> : AbstractValidator<T>
{
    public Func<object, string, Task<IEnumerable<string>>> ValidateValueAsync => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<T>.CreateWithOptions((T)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}
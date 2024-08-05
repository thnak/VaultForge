using FluentValidation;

namespace BusinessModels.Validator;

public class SimpleFluentValueValidator<T> : AbstractValidator<T>
{
    public SimpleFluentValueValidator(Action<IRuleBuilderInitial<T, T>> rule)
    {
        rule(RuleFor(x => x));
    }

    private IEnumerable<string> ValidateValue(T arg)
    {
        var result = Validate(arg);
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    }

    public Func<T, IEnumerable<string>> Validation => ValidateValue;
}
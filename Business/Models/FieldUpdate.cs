using System.Collections;
using System.Linq.Expressions;

namespace Business.Models;

public class FieldUpdate<T> : FieldUpdate
{
    public void Add<TParam>(Expression<Func<T, TParam>> propertyExpression, TParam? value)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        Add(memberExpression.Member.Name, value);
    }

    public TParam? Get<TParam>(Expression<Func<T, TParam>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        return Get<TParam>(memberExpression.Member.Name);
    }

    public TParam? TryGet<TParam>(Expression<Func<T, TParam>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException($"Argument '{nameof(propertyExpression)}' must be a '{nameof(MemberExpression)}'");
        }

        return TryGet<TParam>(memberExpression.Member.Name);
    }
}

public class FieldUpdate : IEnumerable<KeyValuePair<string, object>>
{
    internal Dictionary<string, object?> Parameters { get; set; } = [];


    public void Add(string parameterName, object? value)
    {
        Parameters[parameterName] = value;
    }

    public T? Get<T>(string parameterName)
    {
        if (Parameters.TryGetValue(parameterName, out var value))
        {
            return (T?)value;
        }

        throw new KeyNotFoundException($"{parameterName} does not exist in Dialog parameters");
    }

    public T? TryGet<T>(string parameterName)
    {
        if (Parameters.TryGetValue(parameterName, out var value))
        {
            return (T?)value;
        }

        return default;
    }

    public int Count =>
        Parameters.Count;

    public object? this[string parameterName]
    {
        get => Get<object>(parameterName);
        set => Parameters[parameterName] = value;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return Parameters.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Parameters.GetEnumerator();
    }
}
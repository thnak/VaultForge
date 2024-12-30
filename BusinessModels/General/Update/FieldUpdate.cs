using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

namespace BusinessModels.General.Update;

public class FieldUpdate<T> : FieldUpdate
{
    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        Formatting = Formatting.Indented
    };

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

    public FieldUpdate()
    {
        Parameters = new Dictionary<string, object?>();
        ParameterTypes = new Dictionary<string, string>();
    }

    public FieldUpdate(Dictionary<string, object?> parameters, Dictionary<string, string> parameterTypes)
    {
        Parameters = parameters;
        ParameterTypes = parameterTypes;
    }

    public string GetJson()
    {
        return JsonConvert.SerializeObject(this, _serializerSettings);
    }

    public FieldUpdate SetFromJson(string json)
    {
        return JsonConvert.DeserializeObject<FieldUpdate>(json, _serializerSettings) ?? new FieldUpdate();
    }
}

public class FieldUpdate : IEnumerable<KeyValuePair<string, object>>
{
    public Dictionary<string, object?> Parameters { get; set; } = [];
    public Dictionary<string, string> ParameterTypes { get; set; } = new();


    public FieldUpdate()
    {
    }

    public FieldUpdate(Dictionary<string, object?> parameters, Dictionary<string, string> parameterTypes)
    {
        Parameters = parameters;
        ParameterTypes = parameterTypes;
    }

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

    public int Count => Parameters.Count;

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

public static class FieldUpdateExtensions
{
    public static void UpdateAllFields<T>(this FieldUpdate<T> fieldUpdate, T model)
    {
        if (fieldUpdate == null) throw new ArgumentNullException(nameof(fieldUpdate));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.CanRead) // Ensure the property is readable
            {
                var value = property.GetValue(model);
                fieldUpdate.Add(property.Name, value);
            }
        }
    }
}
using System.Reflection;
using System.Resources;


namespace BusinessModels.Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class LocalizedDisplayNameAttribute(Type resourceType, params string[] resourceKeys) : global::System.Attribute
{
    private readonly ResourceManager _resourceManager = new(resourceType);

    public string GetDescription(int index)
    {
        var description = _resourceManager.GetString(resourceKeys[index]);
        return string.IsNullOrEmpty(description) ? $"[[{resourceKeys[index]}]]" : description;
    }

    public bool HasDescription(int index) => resourceKeys.Length > index;
}

public static class EnumExtensions
{
    public static string GetLocalizedDisplayText(this Enum value, int index = 0)
    {
        var fi = value.GetType().GetField(value.ToString());

        var attribute = (LocalizedDisplayNameAttribute?)fi?.GetCustomAttribute(typeof(LocalizedDisplayNameAttribute), false);

        if (attribute is not null && index >= 0)
        {
            while (!attribute.HasDescription(index) && index != 0)
            {
                index--;
            }

            return attribute.GetDescription(index);
        }

        return value.ToString();
    }
}
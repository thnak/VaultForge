namespace BusinessModels.Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsonDescriptionAttribute(string description) : global::System.Attribute
{
    public string Description { get; } = description;
}

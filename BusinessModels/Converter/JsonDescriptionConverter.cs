using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.Converter;

public class JsonDescriptionConverter<T> : JsonConverter<T> where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Deserialization is not implemented");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (PropertyInfo prop in value.GetType().GetProperties())
        {
            var descriptionAttr = prop.GetCustomAttribute<JsonDescriptionAttribute>();
            string key = descriptionAttr?.Description ?? prop.Name;
            object? propValue = prop.GetValue(value);

            if (propValue != null)
            {
                writer.WritePropertyName(key);
                JsonSerializer.Serialize(writer, propValue, options);
            }
            else
            {
                writer.WriteNull(key);
            }
        }

        writer.WriteEndObject();
    }
}
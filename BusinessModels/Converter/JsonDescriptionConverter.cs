using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.Converter;

public class JsonDescriptionConverter<T> : JsonConverter<T> where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Create an instance of T
        T obj = Activator.CreateInstance<T>();

        // Start reading the JSON object
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return obj;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString();

                // Get the matching property from the object
                PropertyInfo? property = typeToConvert.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<JsonDescriptionAttribute>()?.Description == propertyName || p.Name == propertyName);

                if (property != null)
                {
                    reader.Read();

                    // Deserialize the property value
                    object? propValue = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    property.SetValue(obj, propValue);
                }
                else
                {
                    // Skip the unknown properties if found
                    reader.Skip();
                }
            }
        }

        throw new JsonException("Error deserializing the object.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (PropertyInfo prop in value.GetType().GetProperties())
        {
            var ignoreAttr = prop.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignoreAttr != null)
            {
                continue;
            }

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
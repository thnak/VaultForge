using System.Text.Json;
using System.Text.Json.Serialization;
using MudBlazor.Utilities;

namespace BusinessModels.Converter;

public class MudColorConverter : JsonConverter<MudColor>
{
    public override MudColor? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrEmpty(value) ? default : new MudColor(value);
    }

    public override void Write(Utf8JsonWriter writer, MudColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
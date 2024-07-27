using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace BusinessModels.Converter;

public class ObjectIdConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var objectIdString = reader.GetString();
        return ObjectId.Parse(objectIdString);
    }

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
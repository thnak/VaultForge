using System.Buffers;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MongoDB.Bson;

namespace BusinessModels.Converter;

public class ObjectIdFormatter : IMessagePackFormatter<ObjectId>
{
    public void Serialize(ref MessagePackWriter writer, ObjectId value, MessagePackSerializerOptions options)
    {
        var byteArray = Encoding.UTF8.GetBytes(value.ToString());
        var readOnlyMemory = new ReadOnlyMemory<byte>(byteArray);
        var readOnlySequence = new ReadOnlySequence<byte>(readOnlyMemory);
        writer.WriteString(readOnlySequence);
    }

    public ObjectId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return ObjectId.Parse(reader.ReadString());
    }
}
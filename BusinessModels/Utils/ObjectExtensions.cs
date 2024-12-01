using MessagePack;

namespace BusinessModels.Utils;

public static class ObjectExtensions
{
    public static byte[] Serialize<T>(this T obj) where T : class
    {
        return MessagePackSerializer.Serialize(obj);
    }

    public static Task SerializeAsync<T>(this T obj, Stream outputStream, MessagePackSerializerOptions? options = null, CancellationToken cancellationToken = default) where T : class
    {
        return MessagePackSerializer.SerializeAsync(outputStream, obj, options, cancellationToken);
    }

    public static T Deserialize<T>(this byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MongoDB.Bson;

namespace BusinessModels.Converter;

public class MongoObjectIdResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new MongoObjectIdResolver();

    private MongoObjectIdResolver() { }

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        if (typeof(T) == typeof(ObjectId))
        {
            return (IMessagePackFormatter<T>?)new ObjectIdFormatter();
        }

        if (typeof(T).IsArray && typeof(T).GetElementType() == typeof(ObjectIdFormatter))
        {
            Type? elementType = typeof(T).GetElementType();
            if (elementType != null)
            {
                Type formatterType = typeof(ArrayFormatter<>).MakeGenericType(elementType);
                return (IMessagePackFormatter<T>)Activator.CreateInstance(formatterType)!;
            }

            return null;
        }

        return StandardResolver.Instance.GetFormatter<T>();
    }
}
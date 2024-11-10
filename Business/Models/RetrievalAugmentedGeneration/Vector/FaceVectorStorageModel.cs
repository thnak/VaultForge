using BusinessModels.Base;

namespace Business.Models.RetrievalAugmentedGeneration.Vector;

public class FaceVectorStorageModel : BaseModelEntry
{
    public float[] Vector { get; set; } = new float[512];
    public string Label { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } // Timestamp for when the embedding was added

    public byte[] GetSerializedEmbedding()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        foreach (var value in Vector)
        {
            writer.Write(value);
        }

        return memoryStream.ToArray();
    }

    public static float[] GetDeserializedEmbedding(byte[] data)
    {
        var floatArray = new float[data.Length / sizeof(float)];
        using var memoryStream = new MemoryStream(data);
        using var reader = new BinaryReader(memoryStream);
        for (int i = 0; i < floatArray.Length; i++)
        {
            floatArray[i] = reader.ReadSingle();
        }

        return floatArray;
    }
}
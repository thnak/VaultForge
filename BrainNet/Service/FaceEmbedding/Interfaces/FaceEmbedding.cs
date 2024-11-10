namespace BrainNet.Service.FaceEmbedding.Interfaces;

public interface IFaceEmbedding : IDisposable
{
    public float[] GetEmbeddingArray(Stream stream);
    public float[] GetEmbeddingArray(string imagePath);

}
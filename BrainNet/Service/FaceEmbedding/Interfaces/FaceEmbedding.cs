namespace BrainNet.Service.FaceEmbedding.Interfaces;

public interface IFaceEmbedding : IDisposable
{
    public Task<float[]> GetEmbeddingArray(Stream stream);
}
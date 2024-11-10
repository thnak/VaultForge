using Business.Models.RetrievalAugmentedGeneration.Vector;

namespace Business.Business.Interfaces.User;

public interface IFaceBusinessLayer: IBusinessLayerRepository<FaceVectorStorageModel>, IExtendService, IDisposable, IAsyncDisposable
{
    
}
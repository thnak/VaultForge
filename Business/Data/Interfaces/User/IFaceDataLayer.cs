using Business.Models.RetrievalAugmentedGeneration.Vector;

namespace Business.Data.Interfaces.User;

public interface IFaceDataLayer : IMongoDataInitializer, IDataLayerRepository<FaceVectorStorageModel>
{
    
}
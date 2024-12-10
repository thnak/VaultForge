using Business.Models.RetrievalAugmentedGeneration.Vector;
using BusinessModels.General.Results;

namespace Business.Business.Interfaces.User;

public interface IFaceBusinessLayer: IBusinessLayerRepository<FaceVectorStorageModel>, IExtendService, IDisposable, IAsyncDisposable
{
    Task<Result<bool>> CreateAsync(FaceVectorStorageModel model, float[] vector, CancellationToken cancellationToken = default);
}
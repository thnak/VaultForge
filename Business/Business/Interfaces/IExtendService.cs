using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using BusinessModels.General.Results;

namespace Business.Business.Interfaces;

public interface IExtendService
{
    Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default);
    Task<Result<List<SearchScore<VectorRecord>>>> SearchVectorAsync(float[] vector, int limit = 10, CancellationToken cancellationToken = default);
}
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using BusinessModels.General.Results;

namespace Business.Business.Interfaces;

public interface IExtendService
{
    Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default);
    Task<Result<List<SearchScore<VectorRecord>>?>> SearchVectorAsync(float[] vector, CancellationToken cancellationToken = default);
}
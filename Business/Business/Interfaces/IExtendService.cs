using BusinessModels.General.Results;

namespace Business.Business.Interfaces;

public interface IExtendService
{
    Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default);
}
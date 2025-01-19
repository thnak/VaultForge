using BusinessModels.General.Results;

namespace Business.Data.Interfaces;

public interface IMongoDataInitializer : IDisposable
{
    Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default);
}
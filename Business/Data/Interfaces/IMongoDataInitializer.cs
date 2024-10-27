namespace Business.Data.Interfaces;

public interface IMongoDataInitializer : IDisposable
{
    Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default);
}
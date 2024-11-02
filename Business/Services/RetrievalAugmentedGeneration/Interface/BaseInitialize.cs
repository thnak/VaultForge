namespace Business.Services.RetrievalAugmentedGeneration.Interface;

public interface IBaseInitialize
{
    public Task InitializeAsync(CancellationToken cancellationToken = default);
}
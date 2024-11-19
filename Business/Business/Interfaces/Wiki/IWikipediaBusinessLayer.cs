using BusinessModels.Wiki;

namespace Business.Business.Interfaces.Wiki;

public interface IWikipediaBusinessLayer : IBusinessLayerRepository<WikipediaDatasetModel>, IExtendService, IDisposable, IAsyncDisposable
{
    
}
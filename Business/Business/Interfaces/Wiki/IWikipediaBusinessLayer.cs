using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using BusinessModels.Wiki;

namespace Business.Business.Interfaces.Wiki;

public interface IWikipediaBusinessLayer : IBusinessLayerRepository<WikipediaDatasetModel>, IExtendService, IDisposable, IAsyncDisposable
{
    Task<List<SearchScore<VectorRecord>>> SearchRag(string query, int count, CancellationToken cancellationToken = default);
}
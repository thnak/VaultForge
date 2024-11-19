using BusinessModels.Wiki;

namespace Business.Data.Interfaces.Wiki;

public interface IWikipediaDataLayer : IMongoDataInitializer, IDataLayerRepository<WikipediaDatasetModel>
{
    
}
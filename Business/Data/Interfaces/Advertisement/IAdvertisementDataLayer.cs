using BusinessModels.Advertisement;

namespace Business.Data.Interfaces.Advertisement;

public interface IAdvertisementDataLayer : IMongoDataInitializer, IDataLayerRepository<ArticleModel>
{
    
}
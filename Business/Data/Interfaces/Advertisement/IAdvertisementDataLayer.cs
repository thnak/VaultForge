using BusinessModels.Advertisement;

namespace Business.Data.Interfaces.Advertisement;

public interface IAdvertisementDataLayer : IMongoDataInitializer, IDataLayerRepository<ArticleModel>
{
    /// <summary>
    /// lấy nội dung theo tiêu đề + ngôn ngữ
    /// </summary>
    /// <param name="title"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public ArticleModel? Get(string title, string lang);
}
using BusinessModels.Advertisement;

namespace Business.Business.Interfaces.Advertisement;

public interface IAdvertisementBusinessLayer : IBusinessLayerRepository<ArticleModel>
{
    /// <summary>
    /// lấy nội dung theo tiêu đề + ngôn ngữ
    /// </summary>
    /// <param name="title"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public ArticleModel? Get(string title, string lang);
}
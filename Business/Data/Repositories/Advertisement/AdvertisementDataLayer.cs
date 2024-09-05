using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using BusinessModels.Advertisement;
using MongoDB.Driver;

namespace Business.Data.Repositories.Advertisement;

public class AdvertisementDataLayer(IMongoDataLayerContext context) : IAdvertisementDataLayer
{
    private const string SearchIndexString = "ArticleModelAdvertisements";
    private readonly IMongoCollection<ArticleModel> _dataDb = context.MongoDatabase.GetCollection<ArticleModel>("Article");
    private readonly SemaphoreSlim _semaphore = new(1);

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(FilterDefinition<ArticleModel> filter, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> Where(Expression<Func<ArticleModel, bool>> predicate, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ArticleModel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(ArticleModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }


    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(ArticleModel model, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(ArticleModel model, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }

    public ArticleModel? Get(string title, string lang)
    {
        return _dataDb.Find(x => x.Title == title && x.Language == lang).FirstOrDefault();
    }

    public async Task<(bool, string)> InitializeAsync()
    {
        try
        {
            await _dataDb.Indexes.CreateManyAsync([
                new CreateIndexModel<ArticleModel>(Builders<ArticleModel>.IndexKeys.Ascending(x => x.Title).Ascending(x => x.Language), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<ArticleModel>(Builders<ArticleModel>.IndexKeys.Ascending(x => x.PublishDate), new CreateIndexOptions { Unique = false }),
                new CreateIndexModel<ArticleModel>(Builders<ArticleModel>.IndexKeys.Text(x => x.Title).Text(x => x.Summary), new CreateIndexOptions { Name = SearchIndexString })
            ]);

            Console.WriteLine(@"[Init] Article data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }
}
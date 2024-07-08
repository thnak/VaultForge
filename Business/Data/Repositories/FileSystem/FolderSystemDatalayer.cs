using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.System.FileSystem;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FolderSystemDatalayer(IMongoDataLayerContext context) : IFolderSystemDatalayer
{
    private const string SearchIndexString = "FolderInfoSearchIndex";
    private readonly IMongoCollection<FolderInfoModel> _dataDb = context.MongoDatabase.GetCollection<FolderInfoModel>("FolderInfo");
    private readonly SemaphoreSlim _semaphore = new(1, 1);


    public async Task<(bool, string)> InitializeAsync()
    {
        try
        {
            var keys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.FolderName);
            var indexModel = new CreateIndexModel<FolderInfoModel>(keys);

            var searchIndexKeys = Builders<FolderInfoModel>.IndexKeys.Text(x => x.UserName).Text(x => x.FolderName);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<FolderInfoModel>(searchIndexKeys, searchIndexOptions);
            await _dataDb.Indexes.CreateOneAsync(searchIndexModel);
            await _dataDb.Indexes.CreateOneAsync(indexModel);

            Console.WriteLine(@"[Init] Folder info data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> Search(string queryString, int limit = 10,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10,
        CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> Where(Expression<Func<FolderInfoModel, bool>> predicate,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public FolderInfoModel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(FolderInfoModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models,
        CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }
}
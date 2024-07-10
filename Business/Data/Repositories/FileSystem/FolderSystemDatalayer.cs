using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Data.Repositories.FileSystem;

public class FolderSystemDatalayer(IMongoDataLayerContext context) : IFolderSystemDatalayer
{
    private const string SearchIndexString = "FolderInfoSearchIndex";
    private readonly IMongoCollection<FolderInfoModel> _dataDb = context.MongoDatabase.GetCollection<FolderInfoModel>("FolderInfo");
    private readonly SemaphoreSlim _semaphore = new(1, 1);


    public async Task<(bool, string)> InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var keys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RelativePath);
            var indexModel = new CreateIndexModel<FolderInfoModel>(keys, new CreateIndexOptions() { Unique = true });

            var searchIndexKeys = Builders<FolderInfoModel>.IndexKeys.Text(x => x.FolderName).Text(x => x.RelativePath);
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
        finally
        {
            _semaphore.Release();
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
        var filter = Builders<FolderInfoModel>.Filter.Eq(x => x.RelativePath, key);
        if (ObjectId.TryParse(key, out ObjectId id))
        {
            filter |= Builders<FolderInfoModel>.Filter.Eq(x => x.Id, id);
        }

        return _dataDb.Find(filter).Limit(1).FirstOrDefault();
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

    public async Task<(bool, string)> CreateAsync(FolderInfoModel model)
    {
        await _semaphore.WaitAsync();
        try
        {
            var folder = Get(model.Id.ToString());
            if (folder != null)
            {
                return (false, AppLang.Folder_already_exists);
            }

            await _dataDb.InsertOneAsync(model);
            return (true, AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
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

    public (FolderInfoModel?, string) GetWithPassword(string id, string password)
    {
        var model = Get(id);
        if (model != null)
        {
            if (string.IsNullOrEmpty(password)) return (default, AppLang.Incorrect_password);
            if (model.Password == password.ComputeSha256Hash())
            {
                return (model, AppLang.Success);
            }

            return (default, AppLang.Incorrect_password);
        }

        return (default, AppLang.Folder_could_not_be_found);
    }

    public (bool, string, string) CreateFolder(FolderInfoModel folderInfoModel)
    {
        throw new NotImplementedException();
    }

    public string GetParentFolder(string id)
    {
        throw new NotImplementedException();
    }

    public FolderResult OpenFolder(string id)
    {
        throw new NotImplementedException();
    }

    public (bool, string) ChangeFolderPassword(string id, string password)
    {
        throw new NotImplementedException();
    }
}
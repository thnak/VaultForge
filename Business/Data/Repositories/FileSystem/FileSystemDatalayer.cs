using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FileSystemDatalayer(IMongoDataLayerContext context) : IFileSystemDatalayer
{
    private const string SearchIndexString = "FileInfoSearchIndex";
    private readonly IMongoCollection<FileInfoModel> _fileDataDb = context.MongoDatabase.GetCollection<FileInfoModel>("FileInfo");
    private readonly IMongoCollection<FileMetadataModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileMetadataModel>("FileMetaData");
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<(bool, string)> InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var absolutePathKey = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath);
            var relativePathKey = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath);

            var absolutePathIndexModel = new CreateIndexModel<FileInfoModel>(absolutePathKey, new CreateIndexOptions { Unique = true });
            var relativePathIndexModel = new CreateIndexModel<FileInfoModel>(relativePathKey, new CreateIndexOptions { Unique = true });

            var searchIndexKeys = Builders<FileInfoModel>.IndexKeys.Text(x => x.FileName).Text(x => x.RelativePath);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<FileInfoModel>(searchIndexKeys, searchIndexOptions);
            await _fileDataDb.Indexes.CreateManyAsync([searchIndexModel, absolutePathIndexModel, relativePathIndexModel]);

            Console.WriteLine(@"[Init] File info data layer");

            var metaKey = Builders<FileMetadataModel>.IndexKeys.Ascending(x => x.ThumbnailAbsolutePath);
            var metaIndexModel = new CreateIndexModel<FileMetadataModel>(metaKey, new CreateIndexOptions { Unique = true });
            await _fileMetaDataDataDb.Indexes.CreateOneAsync(metaIndexModel);

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

    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> Where(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public FileInfoModel? Get(string key)
    {
        var filter = Builders<FileInfoModel>.Filter.Eq(x => x.RelativePath, key);
        filter |= Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);

        if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

        return _fileDataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(FileInfoModel model)
    {
        await _semaphore.WaitAsync();
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.ModifiedDate = model.CreatedDate;
                await _fileDataDb.InsertOneAsync(model);
                return (true, AppLang.Create_successfully);
            }
            else
            {
                return (false, AppLang.File_is_already_exsists);
            }
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(FileInfoModel model)
    {
        await _semaphore.WaitAsync();
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                return (false, AppLang.File_could_not_be_found);
            }
            else
            {
                model.CreatedDate = DateTime.UtcNow;
                model.ModifiedDate = model.CreatedDate;
                var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, model.Id);
                await _fileDataDb.ReplaceOneAsync(filter, model);
                return (true, AppLang.Create_successfully);
            }
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

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        _semaphore.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.File_could_not_be_found);

            var query = Get(key);
            if (query == null) return (false, AppLang.File_not_found_);

            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.RelativePath, key);
            filter |= Builders<FileInfoModel>.Filter.Eq(x => x.RelativePath, key);
            if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

            _fileDataDb.DeleteMany(filter);
            File.Delete(query.AbsolutePath);

            DeleteMetadata(query.MetadataId);

            foreach (var extend in query.ExtendResource) Delete(extend.Id);

            return (true, AppLang.Delete_successfully);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public FileMetadataModel? GetMetaData(string metaId)
    {
        if (ObjectId.TryParse(metaId, out var id))
        {
            var filter = Builders<FileMetadataModel>.Filter.Eq(x => x.Id, id);
            return _fileMetaDataDataDb.Find(filter).Limit(1).FirstOrDefault();
        }

        return default;
    }

    public (bool, string) DeleteMetadata(string metaId)
    {
        if (ObjectId.TryParse(metaId, out var id))
        {
            var metadata = GetMetaData(metaId);
            if (metadata == null) return (false, AppLang.Could_not_found_metadata);
            var filter = Builders<FileMetadataModel>.Filter.Eq(x => x.Id, id);
            _fileMetaDataDataDb.DeleteOne(filter);
            File.Delete(metadata.ThumbnailAbsolutePath);
            return (true, AppLang.Delete_successfully);
        }

        return (false, AppLang.Incorrect_metadata_ID);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
}
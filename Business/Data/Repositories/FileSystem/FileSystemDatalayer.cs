using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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

            var absolutePathIndexModel = new CreateIndexModel<FileInfoModel>(absolutePathKey, new CreateIndexOptions { Unique = true });

            var searchIndexKeys = Builders<FileInfoModel>.IndexKeys.Text(x => x.FileName).Text(x => x.RelativePath);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<FileInfoModel>(searchIndexKeys, searchIndexOptions);
            await _fileDataDb.Indexes.CreateManyAsync([searchIndexModel, absolutePathIndexModel]);

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

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FileInfoModel> Where(Expression<Func<FileInfoModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cursor = await _fileDataDb.FindAsync(predicate, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public FileInfoModel? Get(string key)
    {
        var filter = Builders<FileInfoModel>.Filter.Eq(x => x.RelativePath, key);
        filter |= Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);

        if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

        return _fileDataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }


    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(FileInfoModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.ModifiedDate = model.CreatedDate;
                await _fileDataDb.InsertOneAsync(model, cancellationToken: cancellationTokenSource);
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(FileInfoModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
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
                await _fileDataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationTokenSource);
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


    public async IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, [EnumeratorCancellation] CancellationToken cancellationTokenSource = default)
    {
        foreach (var file in models.TakeWhile(_ => cancellationTokenSource is not { IsCancellationRequested: true }))
        {
            var result = await UpdateAsync(file, cancellationTokenSource);
            yield return (true, result.Item2, file.Id.ToString());
        }

        yield return (true, AppLang.Success, string.Empty);
    }

    public (bool, string) Delete(string key)
    {
        _semaphore.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.File_could_not_be_found);

            var query = Get(key);
            if (query == null) return (false, AppLang.File_not_found_);

            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
            if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

            _fileDataDb.DeleteMany(filter);

            try
            {
                File.Delete(query.AbsolutePath);
            }
            catch (Exception)
            {
                //
            }

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
            try
            {
                File.Delete(metadata.ThumbnailAbsolutePath);
            }
            catch (Exception)
            {
                //
            }

            return (true, AppLang.Delete_successfully);
        }

        return (false, AppLang.Incorrect_metadata_ID);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
}
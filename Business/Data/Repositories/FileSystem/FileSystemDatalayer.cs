using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Utils;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FileSystemDatalayer(IMongoDataLayerContext context, ILogger<FileSystemDatalayer> logger) : IFileSystemDatalayer
{
    private const string SearchIndexString = "FileInfoSearchIndex";
    private readonly IMongoCollection<FileInfoModel> _fileDataDb = context.MongoDatabase.GetCollection<FileInfoModel>("FileInfo");
    private readonly IMongoCollection<FileMetadataModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileMetadataModel>("FileMetaData");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
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
            await _fileDataDb.Indexes.CreateManyAsync([searchIndexModel, absolutePathIndexModel], cancellationToken);

            logger.LogInformation(@"[Init] File info data layer");

            var metaKey = Builders<FileMetadataModel>.IndexKeys.Ascending(x => x.ThumbnailAbsolutePath);
            var metaIndexModel = new CreateIndexModel<FileMetadataModel>(metaKey, new CreateIndexOptions { Unique = true });
            await _fileMetaDataDataDb.Indexes.CreateOneAsync(metaIndexModel, cancellationToken: cancellationToken);

            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, null);
            return (false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        var filter = Builders<FileInfoModel>.Filter.Where(f => f.FileName.Contains(keyWord));
        // Build projection
        ProjectionDefinition<FileInfoModel>? projection = fieldsToFetch.ProjectionBuilder();

        // Fetch the documents from the database
        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Projection = projection
        };

        var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<FileInfoModel> Where(Expression<Func<FileInfoModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder()
        };
        var cursor = await _fileDataDb.FindAsync(predicate, options: options, cancellationToken: cancellationToken);
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

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FileInfoModel> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<FileInfoModel>.Filter.Empty;
        var cursor = await _fileDataDb.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FileInfoModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            ObjectId.TryParse(key, out var id);
            var filter = Builders<FileInfoModel>.Filter.Eq(f => f.Id, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<FileInfoModel>.Update;
            var updateDefinitions = new List<UpdateDefinition<FileInfoModel>>();

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    var fieldName = update.Key;
                    var fieldValue = update.Value;

                    // Add the field-specific update to the list
                    updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                }

                // Combine all update definitions into one
                var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                await _fileDataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
            }

            return (true, AppLang.Update_successfully);
        }
        catch (OperationCanceledException e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool, string)> CreateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.ModifiedDate = model.CreatedDate;
                await _fileDataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
                return (true, AppLang.Create_successfully);
            }
            else
            {
                return (false, AppLang.File_is_already_exsists);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
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
                await _fileDataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
                return (true, AppLang.Create_successfully);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }


    public async IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in models.TakeWhile(_ => cancellationToken is not { IsCancellationRequested: true }))
        {
            var result = await UpdateAsync(file, cancellationToken);
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
            catch (Exception e)
            {
                logger.LogError(e, null);
            }

            if (!string.IsNullOrEmpty(query.Thumbnail))
            {
                Delete(query.Thumbnail);
            }

            DeleteMetadata(query.MetadataId);

            foreach (var extend in query.ExtendResource) Delete(extend.Id);

            return (true, AppLang.Delete_successfully);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, null);
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
            catch (Exception e)
            {
                logger.LogError(e, null);
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
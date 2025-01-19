﻿using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Utils;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ArticleModel = BusinessModels.Advertisement.ArticleModel;

namespace Business.Data.Repositories.Advertisement;

public class AdvertisementDataLayer(IMongoDataLayerContext context, ILogger<AdvertisementDataLayer> logger) : IAdvertisementDataLayer
{
    private readonly IMongoCollection<ArticleModel> _dataDb = context.MongoDatabase.GetCollection<ArticleModel>("Article");


    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(FilterDefinition<ArticleModel>.Empty, cancellationToken: cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<ArticleModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(FilterDefinition<ArticleModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ArticleModel> FindProjectAsync(string keyWord, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        var findOptions = fieldsToFetch.Any() ? new FindOptions<ArticleModel, ArticleModel>() { Projection = fieldsToFetch.ProjectionBuilder() } : null;
        var filter = Builders<ArticleModel>.Filter.Where(x => x.Author.Contains(keyWord) || x.Title.Contains(keyWord));

        using var cursor = await _dataDb.FindAsync(filter, findOptions, cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public async IAsyncEnumerable<ArticleModel> WhereAsync(Expression<Func<ArticleModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        var options = fieldsToFetch.Any() ? new FindOptions<ArticleModel, ArticleModel> { Projection = fieldsToFetch.ProjectionBuilder() } : null;
        using var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public ArticleModel? Get(string key)
    {
        try
        {
            if (ObjectId.TryParse(key, out ObjectId objectId))
            {
                return _dataDb.Find(x => x.Id == objectId).FirstOrDefault();
            }

            return _dataDb.Find(x => x.Title == key).FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task<Result<ArticleModel?>> Get(string key, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            var findOptions = fieldsToFetch.Any() ? new FindOptions<ArticleModel, ArticleModel>() { Projection = fieldsToFetch.ProjectionBuilder(), Limit = 1 } : null;
            using var cursor = await _dataDb.FindAsync(x => x.Id == objectId, findOptions);
            var article = cursor.FirstOrDefault();
            if (article != null)
            {
                return Result<ArticleModel?>.Success(article);
            }

            return Result<ArticleModel?>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
        }

        return Result<ArticleModel?>.Failure(AppLang.Invalid_key, ErrorType.Validation);
    }

    public async IAsyncEnumerable<ArticleModel?> GetAsync(List<string> keys, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var key in keys.TakeWhile(_ => cancellationToken.IsCancellationRequested == false))
        {
            yield return Get(key);
        }
    }

    public async Task<(ArticleModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        var skip = page * size;
        long totalCount = await _dataDb.CountDocumentsAsync(FilterDefinition<ArticleModel>.Empty, cancellationToken: cancellationToken);
        var data = await _dataDb.Find(FilterDefinition<ArticleModel>.Empty)
            .Skip(skip)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (data.ToArray(), totalCount);
    }

    public IAsyncEnumerable<ArticleModel> GetAllAsync(Expression<Func<ArticleModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<Result<bool>> UpdateAsync(string key, FieldUpdate<ArticleModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ObjectId.TryParse(key, out var id))
                return Result<bool>.Failure(AppLang.Invalid_key, ErrorType.Validation);

            var filter = Builders<ArticleModel>.Filter.Eq(f => f.Id, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<ArticleModel>.Update;
            var updateDefinitions = new List<UpdateDefinition<ArticleModel>>();
            if (updates.Any())
            {
                updates.Add(model => model.ModifiedTime, DateTime.UtcNow);
                foreach (var update in updates)
                {
                    var fieldName = update.Key;
                    var fieldValue = update.Value;

                    // Add the field-specific update to the list
                    updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                }

                // Combine all update definitions into one
                var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                await _dataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
            }


            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return Result<bool>.Failure(AppLang.Cancel, ErrorType.Cancelled);
        }
    }

    public async Task<Result<bool>> CreateAsync(ArticleModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ArticleModel>.Filter.Where(x => x.Title == model.Title && x.Language == model.Language);
            var isExists = await _dataDb.Find(filter).Limit(1).AnyAsync(cancellationToken: cancellationToken);
            if (isExists)
                return Result<bool>.Failure(AppLang.Article_already_exists, ErrorType.Duplicate);

            model.PublishDate = DateTime.UtcNow.Date;
            model.ModifiedTime = DateTime.UtcNow;
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }


    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<ArticleModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> ReplaceAsync(ArticleModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                file = Get(model.Title, model.Language);
                if (file == null)
                    return Result<bool>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
            }

            model.ModifiedTime = DateTime.UtcNow;
            model.PublishDate = DateTime.UtcNow.Date;
            var filter = Builders<ArticleModel>.Filter.Eq(x => x.Id, model.Id);
            await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (MongoException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
        catch (TaskCanceledException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Cancelled);
        }
    }


    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public ArticleModel? Get(string title, string lang)
    {
        try
        {
            return _dataDb.Find(x => x.Title == title && x.Language == lang).FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataDb.Indexes.CreateManyAsync([
                new CreateIndexModel<ArticleModel>(Builders<ArticleModel>.IndexKeys.Ascending(x => x.Title).Ascending(x => x.Language), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<ArticleModel>(Builders<ArticleModel>.IndexKeys.Ascending(x => x.PublishDate), new CreateIndexOptions { Unique = false }),
            ], cancellationToken);

            const string key = "Index";
            foreach (var culture in AllowedCulture.SupportedCultures)
            {
                ArticleModel? model = Get(key, culture.Name);
                if (model == null)
                {
                    model = new ArticleModel()
                    {
                        Author = "System",
                        Title = "Index",
                        Language = culture.Name,
                        ModifiedTime = DateTime.Now,
                    };
                    var result = await CreateAsync(model, cancellationToken);
                    if (result.IsSuccess)
                    {
                        logger.LogInformation($"[Initialize] Article: {model.Title} - {model.PublishDate} - {model.Language}");
                    }
                    else
                    {
                        logger.LogInformation($"[Initialize Failed] Article: {model.Title} - {model.ModifiedTime} - {model.Language}");
                        logger.LogError(result.Message);
                    }
                }
            }

            logger.LogInformation(@"[Init] Article data layer");
            return Result<bool>.Success(true);
        }
        catch (MongoException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Unknown);
        }
    }

    public void Dispose()
    {
    }
}
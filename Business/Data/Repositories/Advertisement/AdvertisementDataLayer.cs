using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Utils;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ArticleModel = BusinessModels.Advertisement.ArticleModel;

namespace Business.Data.Repositories.Advertisement;

public class AdvertisementDataLayer(IMongoDataLayerContext context, ILogger<AdvertisementDataLayer> logger) : IAdvertisementDataLayer
{
    private const string SearchIndexString = "ArticleModelAdvertisements";
    private readonly IMongoCollection<ArticleModel> _dataDb = context.MongoDatabase.GetCollection<ArticleModel>("Article");
    private readonly SemaphoreSlim _semaphore = new(1);

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
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

    public async IAsyncEnumerable<ArticleModel> FindProjectAsync(string keyWord, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        var projection = new FindOptions<ArticleModel, ArticleModel>()
        {
            Projection = fieldsToFetch.ProjectionBuilder(),
        };

        var filter = Builders<ArticleModel>.Filter.Where(x => x.Author.Contains(keyWord) || x.Title.Contains(keyWord));

        var cursor = await _dataDb.FindAsync(filter, projection, cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public async IAsyncEnumerable<ArticleModel> Where(Expression<Func<ArticleModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        var options = new FindOptions<ArticleModel, ArticleModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder(),
        };
        var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
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
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            return _dataDb.Find(x => x.Id == objectId).FirstOrDefault();
        }

        return _dataDb.Find(x => x.Title == key).FirstOrDefault();
    }

    public async IAsyncEnumerable<ArticleModel?> GetAsync(List<string> keys, [EnumeratorCancellation] CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        foreach (var key in keys.TakeWhile(_ => cancellationTokenSource.IsCancellationRequested == false))
        {
            yield return Get(key);
        }

        _semaphore.Release();
    }

    public async Task<(ArticleModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        var skip = page * size;
        long totalCount = await _dataDb.CountDocumentsAsync(FilterDefinition<ArticleModel>.Empty, cancellationToken: cancellationTokenSource);
        var data = await _dataDb.Find(FilterDefinition<ArticleModel>.Empty)
            .Skip(skip)
            .Limit(size)
            .ToListAsync(cancellationTokenSource);

        return (data.ToArray(), totalCount);
    }

    public IAsyncEnumerable<ArticleModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }


    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(ArticleModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                model.PublishDate = DateTime.UtcNow;
                model.ModifiedDate = model.PublishDate;
                await _dataDb.InsertOneAsync(model, cancellationToken: cancellationTokenSource);
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


    public async IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<ArticleModel> models, [EnumeratorCancellation] CancellationToken cancellationTokenSource = default)
    {
        foreach (var model in models.TakeWhile(_ => cancellationTokenSource.IsCancellationRequested == false))
        {
            var result = await CreateAsync(model, cancellationTokenSource);
            yield return (result.Item1, result.Item2, "");
        }
    }

    public async Task<(bool, string)> UpdateAsync(ArticleModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                file = Get(model.Title, model.Language);
                if (file == null)
                    return (false, AppLang.File_could_not_be_found);
            }

            model.ModifiedDate = DateTime.UtcNow;
            var filter = Builders<ArticleModel>.Filter.Eq(x => x.Id, model.Id);
            await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationTokenSource);
            return (true, AppLang.Update_successfully);
        }
        catch (MongoException e)
        {
            return (false, e.Message);
        }
        catch (TaskCanceledException e)
        {
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
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

            logger.LogInformation(@"[Init] Article data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }
}
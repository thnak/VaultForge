using System.Linq.Expressions;
using Business.Models;
using BusinessModels.People;
using MongoDB.Driver;

namespace Business.Data.Interfaces.Entity.User;

public class UserEntityDataLayer(EntityDataContext entityDataContext) : IDataLayerRepository<UserModel>
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public UserModel? Get(string key)
    {
        return entityDataContext.UserContext.FirstOrDefault(x => x.UserName == key);
    }

    public IAsyncEnumerable<UserModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<UserModel> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<UserModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<UserModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}
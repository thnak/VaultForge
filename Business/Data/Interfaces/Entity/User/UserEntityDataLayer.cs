using System.Linq.Expressions;
using BusinessModels.People;
using MongoDB.Driver;

namespace Business.Data.Interfaces.Entity.User;

public class UserEntityDataLayer(EntityDataContext entityDataContext) : IDataLayerRepository<UserModel>
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
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

    public IAsyncEnumerable<UserModel?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdatePropertiesAsync(string key, Dictionary<Expression<Func<UserModel, object>>, object> updates, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(UserModel model, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<UserModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(UserModel model, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }


    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }
}
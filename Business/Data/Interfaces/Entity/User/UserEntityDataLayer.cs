using System.Linq.Expressions;
using BusinessModels.People;
using MongoDB.Driver;

namespace Business.Data.Interfaces.Entity.User;

public class UserEntityDataLayer(EntityDataContext entityDataContext) : IDataLayerRepository<UserModel>
{
    public Task<long> GetDocumentSizeAsync(CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10, CancellationTokenSource? cancellationTokenSource = default)
    {
        var data = entityDataContext.UserContext.Where(x => x.UserName.Contains(queryString));
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        var data = entityDataContext.UserContext.Where(predicate);
        throw new NotImplementedException();
    }

    public UserModel? Get(string key)
    {
        return entityDataContext.UserContext.FirstOrDefault(x => x.UserName == key);
    }

    public IAsyncEnumerable<UserModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(UserModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<UserModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(UserModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }
}
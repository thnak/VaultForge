using System.Globalization;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.User;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.Secure;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Business.Repositories.User;

public class UserBusinessLayer(IUserDataLayer userDl) : IUserBusinessLayer
{
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
    public UserModel? Get(string key)
    {
        return userDl.Get(key);
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
    public (bool, string) Authenticate(RequestLoginModel model)
    {
        var user = Get(model.UserName);
        if (user == null) return (false, AppLang.Username_or_password_incorrect);
        if (user.PasswordHash == model.Password.ComputeSha256Hash())
        {
            return (true, AppLang.User_has_been_authenticated);
        }
        return (false, AppLang.Username_or_password_incorrect);
    }
    public ClaimsIdentity CreateIdentity(string userName)
    {
        var user = Get(userName);
        if (user == null) return new ClaimsIdentity();

        var claims = GetAllClaim(user);

        return new ClaimsIdentity(claims, CookieNames.AuthenticationType);
    }
    public List<Claim> GetAllClaim(string userName)
    {
        var user = Get(userName);
        return user == null ? [] : GetAllClaim(user);
    }
    public List<Claim> GetAllClaim(UserModel user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserName),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Hash, user.SecurityStamp),
            new(ClaimTypes.DateOfBirth, user.BirthDay.ToString(CultureInfo.InvariantCulture)),

        };
        var roles = userDl.GetAllRoles(user.UserName);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}
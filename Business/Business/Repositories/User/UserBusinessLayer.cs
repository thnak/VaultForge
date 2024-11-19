using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.User;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.Secure;
using BusinessModels.Utils;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Business.Repositories.User;

public class UserBusinessLayer(IUserDataLayer userDl) : IUserBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return userDl.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return userDl.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return userDl.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter, CancellationToken cancellationToken = default)
    {
        return userDl.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return userDl.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        return userDl.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }


    public IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        return userDl.Where(predicate, cancellationToken, fieldsToFetch);
    }

    public UserModel? Get(string key)
    {
        return userDl.Get(key);
    }

    public Task<Result<UserModel?>> Get(string key, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        return userDl.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<UserModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return userDl.GetAsync(keys, cancellationToken);
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return userDl.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(Expression<Func<UserModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return userDl.GetAllAsync(field2Fetch, cancellationToken);
    }


    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<UserModel> updates, CancellationToken cancellationToken = default)
    {
        return userDl.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        return userDl.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<UserModel> models, CancellationToken cancellationToken = default)
    {
        return userDl.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        return userDl.ReplaceAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models, CancellationToken cancellationToken = default)
    {
        return userDl.ReplaceAsync(models, cancellationToken);
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        return userDl.DeleteAsync(key, cancelToken);
    }

    public UserModel GetAnonymous()
    {
        return userDl.GetAnonymous();
    }

    public (bool, string) Authenticate(RequestLoginModel model)
    {
        var user = Get(model.UserName);
        if (user == null) return (false, AppLang.Username_or_password_incorrect);

        try
        {
            if (user.BanTime > DateTime.UtcNow) return (false, AppLang.You_have_been_banned_from_logging_in__please_try_again_at_X0.AutoReplace([user.BanTime.ToLocalTime().ToString(CultureInfo.CurrentCulture)]));

            if (user.Password == model.Password.ComputeSha256Hash())
            {
                user.CurrentFailCount = 0;
                user.AccessFailedCount = 0;
                user.BanTime = DateTime.MinValue;
                return (true, AppLang.User_has_been_authenticated);
            }

            user.CurrentFailCount++;
            if (user.CurrentFailCount >= 3)
            {
                var banMinus = Math.Pow(5, user.AccessFailedCount);
                user.BanTime = DateTime.UtcNow.AddMinutes(banMinus);
                user.AccessFailedCount++;
                user.CurrentFailCount = 0;
            }

            return (false, AppLang.Username_or_password_incorrect);
        }
        finally
        {
            UpdateAsync(user);
        }
    }

    public (bool, string) ValidateUsername(string username)
    {
        var user = userDl.Get(username);
        return user == null ? (false, AppLang.User_is_not_exists) : (true, AppLang.Hello);
    }

    public (bool, string) ValidatePassword(string username, string password)
    {
        var user = userDl.Get(username);
        if (user == null) return (false, AppLang.User_is_not_exists);

        if (user.BanTime > DateTime.UtcNow) return (false, AppLang.You_have_been_banned_from_logging_in__please_try_again_at_X0.AutoReplace([user.BanTime.ToLocalTime().ToString(CultureInfo.CurrentCulture)]));

        if (user.Password == password.ComputeSha256Hash()) return (true, AppLang.Hello);

        user.CurrentFailCount++;
        if (user.CurrentFailCount >= 3)
        {
            var banMinus = Math.Pow(5, user.AccessFailedCount);
            user.BanTime = DateTime.UtcNow.AddMinutes(banMinus);
            user.AccessFailedCount++;
            user.CurrentFailCount = 0;
        }

        UpdateAsync(user);
        return (false, AppLang.User_or_password_is_incorrect);
    }

    public ClaimsIdentity CreateIdentity(string userName)
    {
        var user = Get(userName);
        if (user == null) return new ClaimsIdentity();
        var claims = GetAllClaim(user, userName);
        return new ClaimsIdentity(claims, CookieNames.AuthenticationType);
    }

    public List<Claim> GetAllClaim(string userName)
    {
        var user = Get(userName);
        return user == null ? [] : GetAllClaim(user, userName);
    }

    public List<Claim> GetAllClaim(UserModel user, string userName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserName), // Hashed
            new(ClaimTypes.Name, userName), // normal string
            new(ClaimTypes.Hash, user.SecurityStamp),
            new(ClaimTypes.DateOfBirth, user.BirthDay.ToString(CultureInfo.InvariantCulture))
        };
        var roles = userDl.GetAllRoles(userName);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}
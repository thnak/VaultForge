using System.Globalization;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.User;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.Secure;
using BusinessModels.Utils;
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
        return userDl.UpdateAsync(model);
    }
    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        return userDl.UpdateAsync(models, cancellationTokenSource);
    }
    public (bool, string) Delete(string key)
    {
        return userDl.Delete(key);
    }
    public (bool, string) Authenticate(RequestLoginModel model)
    {
        var userNameHash = model.UserName.ComputeSha256Hash();
        var user = Get(userNameHash);
        if (user == null) return (false, AppLang.Username_or_password_incorrect);

        try
        {
            if (user.BanTime > DateTime.Now)
            {
                return (false, AppLang.You_have_been_banned_from_logging_in__please_try_again_at__0_.AutoReplace([user.BanTime.ToString(CultureInfo.CurrentCulture)]));
            }

            if (user.Password == model.Password.ComputeSha256Hash() && user.UserName == userNameHash)
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
                user.BanTime = DateTime.Now.AddMinutes(banMinus);
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
    public ClaimsIdentity CreateIdentity(string userName)
    {
        var user = Get(userName.ComputeSha256Hash());
        if (user == null) return new ClaimsIdentity();

        var claims = GetAllClaim(user, userName);

        return new ClaimsIdentity(claims, CookieNames.AuthenticationType);
    }
    public List<Claim> GetAllClaim(string userName)
    {
        var user = Get(userName.ComputeSha256Hash());
        return user == null ? [] : GetAllClaim(user, userName);
    }
    public List<Claim> GetAllClaim(UserModel user, string userName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserName),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Hash, user.SecurityStamp),
            new(ClaimTypes.DateOfBirth, user.BirthDay.ToString(CultureInfo.InvariantCulture)),
        };
        var roles = userDl.GetAllRoles(user.UserName);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}
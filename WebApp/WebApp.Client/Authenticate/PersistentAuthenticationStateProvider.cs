using System.Security.Claims;
using BusinessModels.People;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.Client.Authenticate;

public class PersistentAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask = DefaultUnauthenticatedTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<UserInfoModel>(nameof(UserInfoModel), out var userInfo) || userInfo is null) return;

        AvatarUri = userInfo.Avatar;
        UserName = userInfo.UserName;
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserName),
            new Claim(ClaimTypes.Name, userInfo.UserName),
            new Claim(ClaimTypes.Email, userInfo.Email)
        ];
        claims.AddRange(userInfo.Roles.Select(x => new Claim(ClaimTypes.Role, x)));

        _authenticationStateTask = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieNames.AuthenticationType))));
    }

    public string UserName { get; set; } = string.Empty;
    public string AvatarUri { get; set; } = "default_user.jpg";

    public void Dispose()
    {
        _authenticationStateTask.Dispose();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return _authenticationStateTask;
    }
}
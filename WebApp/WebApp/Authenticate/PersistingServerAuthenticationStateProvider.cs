using System.Diagnostics;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using BusinessModels.People;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace WebApp.Authenticate;

public class PersistingServerAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly IdentityOptions _options;
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly Lazy<IUserBusinessLayer> _userBl;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState persistentComponentState,
        IOptions<IdentityOptions> optionsAccessor,
        IServiceProvider serviceProvider)
    {
        _state = persistentComponentState;
        _options = optionsAccessor.Value;
        _userBl = new Lazy<IUserBusinessLayer>(serviceProvider.GetRequiredService<IUserBusinessLayer>);

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    public void Dispose()
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _authenticationStateTask = task;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null) throw new UnreachableException($"Authentication state not set in {nameof(OnPersistingAsync)}().");

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            var originUsername = principal.FindFirst(_options.ClaimsIdentity.UserNameClaimType)?.Value;
            var email = principal.FindFirst(_options.ClaimsIdentity.EmailClaimType)?.Value;

            if (originUsername != null)
            {
                var user = _userBl.Value.Get(originUsername);
                if (user != null)
                {
                    var avatarUri = user.Avatar;
                    _state.PersistAsJson(nameof(UserInfoModel), new UserInfoModel
                    {
                        UserName = originUsername,
                        Email = email ?? string.Empty,
                        Roles = user.Roles,
                        Avatar = avatarUri,
                        JwtAccessToken = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value ?? string.Empty
                    });
                }
            }
        }
    }
}
using System.Diagnostics;
using Business.Business.Interfaces.User;
using BusinessModels.People;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CodeWithMe.Authenticate;

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
            var userId = principal.FindFirst(_options.ClaimsIdentity.UserIdClaimType)?.Value;
            var email = principal.FindFirst(_options.ClaimsIdentity.EmailClaimType)?.Value;

            if (userId != null)
            {
                var roles = new List<string>();
                var avatarUri = string.Empty;
                if (!string.IsNullOrEmpty(principal.Identity.Name))
                {
                    avatarUri = _userBl.Value.Get(principal.Identity.Name)?.ImageUrl ?? avatarUri;
                }

                _state.PersistAsJson(nameof(UserInfoModel), new UserInfoModel
                {
                    UserName = userId,
                    Email = email ?? string.Empty,
                    Roles = roles,
                    ImageUrl = avatarUri
                });
            }
        }
        _state.PersistAsJson(nameof(UserInfoModel), new UserInfoModel
        {
            UserName = "haha",
            Email = "haha",
            Roles = [],
            ImageUrl = "avatarUri"
        });
    }
}
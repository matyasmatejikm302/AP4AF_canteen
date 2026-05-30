using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using UTB.Minute.Web.Client;

namespace UTB.Minute.Web;

public class PersistingServerAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;
    private Task<AuthenticationState>? _authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        PersistentComponentState state)
        : base(loggerFactory)
    {
        _state = state;

        AuthenticationStateChanged += OnAuthenticationStateChanged;

        _subscription = state.RegisterOnPersisting(OnPersisting);
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _authenticationStateTask = task;
    }

    private async Task OnPersisting()
    {
        if (_authenticationStateTask is null)
        {
            throw new UnreachableException($"Authentication state not set in {nameof(OnPersisting)}.");
        }

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? principal.FindFirst("sub")?.Value
                      ?? string.Empty;

            var name = principal.Identity.Name
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? string.Empty;

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            _state.PersistAsJson(nameof(UserInfo), new UserInfo
            {
                UserId = userId,
                Name = name,
                Roles = roles
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing);
    }
}
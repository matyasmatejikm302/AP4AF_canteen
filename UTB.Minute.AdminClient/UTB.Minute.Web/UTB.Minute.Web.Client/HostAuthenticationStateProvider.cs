using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UTB.Minute.Web.Client;

public class HostAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> DefaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask = DefaultUnauthenticatedTask;

    public HostAuthenticationStateProvider(PersistentComponentState state)
    {
        if (state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) && userInfo is not null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
                new Claim(ClaimTypes.Name, userInfo.Name)
            };

            foreach (var role in userInfo.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "KeycloakAuth");
            var principal = new ClaimsPrincipal(identity);
            _authenticationStateTask = Task.FromResult(new AuthenticationState(principal));
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;
}
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace UTB.Minute.Web.Client;

public class HostAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Prozatím pro testování hardcodujeme přihlášeného studenta.
        // Tím obelstíme klientský Blazor, aby si nemyslel, že musí startovat RemoteAuthenticationService,
        // a zároveň budeme mít pro WebApi platného uživatele "Matyáš Matějík".
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Matyáš Matějík"),
            new Claim(ClaimTypes.Role, "Student")
        }, "KeycloakAuth");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
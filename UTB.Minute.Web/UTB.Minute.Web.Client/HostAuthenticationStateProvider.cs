using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace UTB.Minute.Web.Client;

public class HostAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Pro lokální vývoj ti přiřadíme roli studenta i kuchaře najednou,
        // takže uvidíš v menu Jídelnu i Kuchyni a můžeš vesele testovat.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Matyáš Matějík"),
            new Claim(ClaimTypes.Role, "Student"),
            new Claim(ClaimTypes.Role, "Cook") // Přidána role Cook
        }, "KeycloakAuth");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Výchozí konfigurace .NET Aspire (metriky, logování, service discovery)
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Zabezpečení: Autorizace a kaskádní stav přihlášení pro Blazor komponenty
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// HttpClient nakonfigurovaný přes Aspire Service Discovery k volání WebApi
builder.Services.AddHttpClient("webapi", client =>
{
    client.BaseAddress = new Uri("http://webapi");
});

// Registrace přihlášení pomocí cookies a Keycloaku (OIDC) přes Aspire s mapováním rolí
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddKeycloakOpenIdConnect("keycloak", "Canteen", options =>
{
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                // Keycloak posílá role jako JSON strukturu v claimu "realm_access"
                var realmAccessClaim = identity.FindFirst("realm_access");
                if (realmAccessClaim != null)
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(realmAccessClaim.Value);
                        if (jsonDoc.RootElement.TryGetProperty("roles", out var rolesElement) &&
                            rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var roleElement in rolesElement.EnumerateArray())
                            {
                                var roleName = roleElement.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    // Namapujeme roli jako standardní .NET ClaimTypes.Role
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignorujeme případné chyby při parsování
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
using UTB.Minute.Web.Components;
using Yarp.ReverseProxy.Forwarder;
using Aspire.Keycloak.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpForwarder();

// --- OpenID Connect (Keycloak) configuration ---
// Default demo values — replace with your Keycloak server settings in production
var keycloakRealm = builder.Configuration["Keycloak:Realm"] ?? "utb-school";
var keycloakClientId = builder.Configuration["Keycloak:ClientId"] ?? "utb-minute-web";
var keycloakClientSecret = builder.Configuration["Keycloak:ClientSecret"] ?? "CHANGE_ME";
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "https://localhost:8443/realms/utb-school";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: keycloakRealm,
        options =>
        {
            options.ClientId = keycloakClientId;
            options.ClientSecret = keycloakClientSecret; // for demo only
            options.Authority = keycloakAuthority;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("roles");
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false; // DEV only
            options.TokenValidationParameters.NameClaimType = "preferred_username";
        });

builder.Services.AddAuthorization();

// Supply AuthenticationState to Razor components
builder.Services.AddCascadingAuthenticationState();

// Register HttpClient so Blazor components can inject System.Net.Http.HttpClient
// Use configured API address if present, otherwise fallback to localhost.
// Try to read API address from configuration, otherwise fallback to local WebApi dev URL
var apiAddress = builder.Configuration["services:webapi:http:0"]
              ?? builder.Configuration["services:webapi:https:0"];

// If not configured, assume local WebApi development port (from launchSettings)
if (string.IsNullOrEmpty(apiAddress))
{
    apiAddress = "https://localhost:7008";
}

if (!string.IsNullOrEmpty(apiAddress) && !apiAddress.EndsWith("/")) apiAddress += "/";

builder.Services.AddScoped<System.Net.Http.HttpClient>(sp =>
    new System.Net.Http.HttpClient { BaseAddress = new Uri(apiAddress) });

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

// Aktivace zabezpečení
app.UseAuthentication();
app.UseAuthorization();

// --- Login / Logout endpoints for OIDC ---
app.MapGet("/login", async (HttpContext ctx, string? returnUrl) =>
{
    string redirectUri = "/";
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        redirectUri = returnUrl!;
    }

    await ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = redirectUri,
        IsPersistent = false
    });
});

app.MapPost("/logout", async (HttpContext ctx) =>
{
    string? idToken = await ctx.GetTokenAsync("id_token");

    // Sign out locally and at the identity provider
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/",
        Parameters = { { "id_token_hint", idToken ?? string.Empty } }
    });
});

// --- PROXY LOGIKA ---
if (!string.IsNullOrEmpty(apiAddress))
{
    // Proxy teď bude automaticky přeposílat "Authorization" hlavičku s tokenem
    app.MapForwarder("/menu/{**catch-all}", apiAddress);
    app.MapForwarder("/orders/{**catch-all}", apiAddress);
    app.MapForwarder("/orders", apiAddress);
    app.MapForwarder("/meals/{**catch-all}", apiAddress);
}

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UTB.Minute.Web.Client._Imports).Assembly);

app.Run();

using Aspire.Keycloak.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using UTB.Minute.Contracts;
using UTB.Minute.Web;
using UTB.Minute.Web.Client.Pages;
using UTB.Minute.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// --- OPRAVA CHYBY HTTP 431 ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 1024 * 64;
});

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpForwarder();

// --- 2. DYNAMICKÉ NASTAVENÍ PRO REALM CANTEEN ---
var keycloakBaseUrl = builder.Configuration["services:keycloak:https:0"]
                   ?? builder.Configuration["services:keycloak:http:0"]
                   ?? "http://keycloak";

var keycloakRealm = "Canteen";
var keycloakClientId = "canteen-client";
//var keycloakClientSecret = "qP9SkJimrAgh2l31VTy7Xgh3O7Jv4X1B";
var keycloakAuthority = $"{keycloakBaseUrl}/realms/{keycloakRealm}";

// --- 3. OpenID Connect (Keycloak) konfigurace s mapováním rolí ---
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
            //options.ClientSecret = keycloakClientSecret;
            options.Authority = keycloakAuthority;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("roles");
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false;
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            options.TokenValidationParameters.NameClaimType = "preferred_username";

            // Mapování vnořených rolí z Keycloaku na C# Claims
            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
                        if (!string.IsNullOrEmpty(realmAccessClaim))
                        {
                            try
                            {
                                using var jsonDoc = System.Text.Json.JsonDocument.Parse(realmAccessClaim);
                                if (jsonDoc.RootElement.TryGetProperty("roles", out var rolesArray))
                                {
                                    foreach (var role in rolesArray.EnumerateArray())
                                    {
                                        var roleName = role.GetString();
                                        if (!string.IsNullOrEmpty(roleName))
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Ignorovat chyby
                            }
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Registrace našeho serverového provideru, který serializuje stav a předává ho do klientského WebAssembly
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

// HttpClient konfigurace
var apiAddress = builder.Configuration["services:webapi:http:0"]
              ?? builder.Configuration["services:webapi:https:0"]
              ?? "http://webapi";

if (!apiAddress.EndsWith("/")) apiAddress += "/";

builder.Services.AddHttpClient("webapi", client =>
{
    client.BaseAddress = new Uri(apiAddress);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("webapi"));

var app = builder.Build();

app.MapStaticAssets();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.MapStaticAssets();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// --- 4. LOGIN / LOGOUT ---
app.MapGet("/login", async (HttpContext ctx, string? returnUrl) =>
{
    string redirectUri = "/";
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        redirectUri = returnUrl;
    }
    await ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = redirectUri,
        IsPersistent = false
    });
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

// --- 5. ČISTÝ C# BFF PROXY PRO WEBASSEMBLY KLIENTA ---
app.MapGet("/menu", async (HttpClient client) =>
{
    var result = await client.GetFromJsonAsync<MenuItemDto[]>("/menu");
    return Results.Ok(result);
});

app.MapGet("/orders", async (HttpClient client) =>
{
    var result = await client.GetFromJsonAsync<OrderDto[]>("/orders");
    return Results.Ok(result);
});

app.MapPost("/orders", async (CreateOrderDto req, HttpClient client) =>
{
    var response = await client.PostAsJsonAsync("/orders", req);
    if (response.IsSuccessStatusCode)
    {
        var dto = await response.Content.ReadFromJsonAsync<OrderDto>();
        return Results.Created($"/orders/{dto?.Id}", dto);
    }
    return Results.BadRequest(await response.Content.ReadAsStringAsync());
});

app.MapPatch("/orders/{id:guid}/state", async (Guid id, ChangeOrderStateDto req, HttpClient client) =>
{
    var response = await client.PatchAsJsonAsync($"/orders/{id}/state", req);
    if (response.IsSuccessStatusCode)
    {
        return Results.NoContent();
    }
    return Results.BadRequest();
});

app.MapGet("/orders/sse", async (HttpContext context, HttpClient client, CancellationToken ct) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    try
    {
        using var response = await client.GetAsync("/orders/sse", HttpCompletionOption.ResponseHeadersRead, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            await context.Response.WriteAsync(line + "\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }
    catch (OperationCanceledException) { }
});

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UTB.Minute.Web.Client.Pages.Canteen).Assembly);

app.Run();
using UTB.Minute.Web.Components;
using Aspire.Keycloak.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using UTB.Minute.Contracts;
using UTB.Minute.Web.Client.Pages; // Nezbytné pro odkaz na stránku Canteen

var builder = WebApplication.CreateBuilder(args);

// 1. Nastavení standardních Aspire a Blazor služeb
builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// --- 2. DYNAMICKÉ NASTAVENÍ PRO REALM UTB ---
// PŘEDNOSTNĚ voláme "https", pro které máme nastavený SSL validator níže
var keycloakBaseUrl = builder.Configuration["services:keycloak:https:0"]
                   ?? builder.Configuration["services:keycloak:http:0"]
                   ?? "http://keycloak";

var keycloakRealm = "UTB";
var keycloakClientId = "canteen-web";

// TVŮJ CLIENT SECRET Z KEYCLOAKU:
var keycloakClientSecret = "28gmH9XPxhhBRMRyIcthhq6f7kcCieA1";

var keycloakAuthority = $"{keycloakBaseUrl}/realms/{keycloakRealm}";

// --- 3. OpenID Connect (Keycloak) konfigurace ---
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
            options.ClientSecret = keycloakClientSecret;
            options.Authority = keycloakAuthority;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("roles");
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false; // Pouze pro lokální vývoj

            // Deaktivace PAR protokolu (řeší chyby s přihlášením klienta na pozadí)
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

            // Ignorování chyb neplatných SSL certifikátů v Dockeru
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            options.TokenValidationParameters.NameClaimType = "preferred_username";
        });

builder.Services.AddAuthorization();

// Předání stavu přihlášení do Razor komponent
builder.Services.AddCascadingAuthenticationState();

// Konfigurace HttpClient pro vnitřní volání z webového serveru na WebApi
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

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

// app.UseHttpsRedirection(); // <--- ZAKOMENTOVÁNO (umožní kliknout na HTTP port 5008 v dashboardu)
app.MapStaticAssets();
app.UseAntiforgery();

// Aktivace přihlašování a zabezpečení
app.UseAuthentication();
app.UseAuthorization();

// --- 4. LOGIN / LOGOUT ENDPOINTY PRO OIDC ---
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

// --- 5. ČISTÝ C# BFF PROXY (PŘEPOSÍLÁNÍ DOTAZŮ BEZ CHYB YARPU) ---
// Proxy pro Menu (GET)
app.MapGet("/menu", async (HttpClient client) =>
{
    var result = await client.GetFromJsonAsync<MenuItemDto[]>("/menu");
    return Results.Ok(result);
});

// Proxy pro Objednávky (POST)
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

// Proxy pro real-time SSE stream (GET)
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

// --- 6. REGISTRACE KOMPONENT S EXPLICITNÍ REFERENCÍ NA CANTEEN PAGE ---
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UTB.Minute.Web.Client.Pages.Canteen).Assembly); // <--- Změněno na Canteen

app.Run();
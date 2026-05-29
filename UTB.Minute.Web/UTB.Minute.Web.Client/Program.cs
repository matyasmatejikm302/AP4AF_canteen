using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure OIDC authentication (Keycloak) via configuration in wwwroot/appsettings.json
builder.Services.AddOidcAuthentication(options =>
{
    // Options will be bound from configuration (wwwroot/appsettings.json)
});

// HttpClient for WebAPI calls (will use the access token)
builder.Services.AddHttpClient("webapi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

// Configure default client that uses token
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("webapi"));

await builder.Build().RunAsync();

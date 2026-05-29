using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization; // Přidat using
using UTB.Minute.Web.Client; // Přidat using

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nastavení HttpClienta
builder.Services.AddHttpClient("webapi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("webapi"));

// --- NOVINKA: Registrace našeho vlastního ověřovatele ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, HostAuthenticationStateProvider>();

await builder.Build().RunAsync();
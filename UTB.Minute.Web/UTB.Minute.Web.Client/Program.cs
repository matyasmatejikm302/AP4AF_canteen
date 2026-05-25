using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nastavíme HttpClient tak, aby volal adresu, na které běží web v prohlížeči
builder.Services.AddHttpClient("webapi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Zaregistrujeme ho jako výchozí
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("webapi"));

await builder.Build().RunAsync();
using UTB.Minute.Web.Components;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpForwarder();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

// app.UseHttpsRedirection(); // Pro lokální vývoj s proxy je lepší toto dočasně zakomentovat
app.MapStaticAssets();
app.UseAntiforgery();

// --- ROBUSTNÍ ASPIRE PROXY ---
// Aspire automaticky vloží adresu backendu do konfigurace pod názvem "services:webapi:http:0"
// nebo "services:webapi:https:0"
var apiAddress = builder.Configuration["services:webapi:http:0"]
              ?? builder.Configuration["services:webapi:https:0"];

if (!string.IsNullOrEmpty(apiAddress))
{
    // Musíme zajistit, aby adresa končila lomítkem
    if (!apiAddress.EndsWith("/")) apiAddress += "/";

    app.MapForwarder("/menu/{**catch-all}", apiAddress);
    app.MapForwarder("/orders/{**catch-all}", apiAddress);
    app.MapForwarder("/orders", apiAddress);
    app.MapForwarder("/meals/{**catch-all}", apiAddress);
}

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UTB.Minute.Web.Client._Imports).Assembly);

app.Run();
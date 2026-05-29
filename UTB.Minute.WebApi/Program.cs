using UTB.Minute.Db;
using UTB.Minute.WebApi.Endpoints;
using UTB.Minute.WebApi.Services;
using Aspire.Keycloak.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Registrace DB
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

// --- NOVINKA: Nastavení ověřování přes Keycloak ---
// Aspire si sám vytáhne adresu Keycloaku z AppHostu
builder.AddKeycloakJwtAuthentication("keycloak");

builder.Services.AddSingleton<SseService>();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Přidáme podporu autorizace (umožňuje používat atribut [Authorize])
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseCors();

// --- NOVINKA: Aktivace zabezpečení ---
// Program.cs (po builder.Build())
if (builder.Configuration.GetSection("keycloak").Exists())
{
    app.UseAuthentication();
    app.UseAuthorization();
}
else
{
    // Volitelně: logovat varování, že auth není povolena
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Keycloak configuration not found; authentication middleware not enabled.");
}

app.MapMealEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

app.Run();
using UTB.Minute.Db;
using UTB.Minute.WebApi.Endpoints;
using UTB.Minute.WebApi.Services;
using Aspire.Keycloak.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. Registrace PostgreSQL databáze
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

// 2. Ověřování tokenů přes Keycloak
builder.AddKeycloakJwtAuthentication("keycloak");

// 3. Registrace SSE služby jako Singleton
builder.Services.AddSingleton<SseService>();

// 4. Nastavení CORS pro komunikaci s frontendem
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 5. Podpora pro autorizaci
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

// POZNÁMKA: Ponecháváme zakomentované, aby se proxy netloukla s HTTPS přesměrováním
// app.UseHttpsRedirection(); 

app.UseCors();

// 6. Aktivace zabezpečení na backendu (pouze pokud existuje konfigurace pro Keycloak)
if (builder.Configuration.GetSection("keycloak").Exists())
{
    app.UseAuthentication();
    app.UseAuthorization();
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Keycloak configuration not found; authentication middleware not enabled.");
}

// 7. Registrace našich endpointů
app.MapMealEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

app.Run();
using UTB.Minute.Db;
using UTB.Minute.WebApi.Endpoints;
using UTB.Minute.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Aspire Service Defaults (Telemetrie, Health Checks, Service Discovery)
// Tento řádek je kritický, aby Aspire věděl, na jakých portech API běží
builder.AddServiceDefaults();

// 2. Registrace databázového kontextu pro PostgreSQL
// Název "CanteenDb" musí odpovídat názvu v AppHostu
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

// 3. Registrace SSE služby jako Singleton (jedna ústředna pro celou aplikaci)
builder.Services.AddSingleton<SseService>();

// 4. Konfigurace CORS (Cross-Origin Resource Sharing)
// Povolujeme komunikaci z prohlížeče, aby Proxy i přímá volání fungovala bez chyb
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 5. Výchozí Aspire endpointy
app.MapDefaultEndpoints();

// 6. HTTPS přesměrování
// POZNÁMKA: Pokud proxy v AppHostu stále hlásí 502, můžete tento řádek zakomentovat,
// aby vnitřní komunikace mezi kontejnery probíhala čistě přes HTTP.
// app.UseHttpsRedirection();

// 7. Aktivace CORS
app.UseCors();

// 8. Registrace skupin endpointů z projektu WebApi.Endpoints
app.MapMealEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

// 9. Spuštění aplikace
app.Run();
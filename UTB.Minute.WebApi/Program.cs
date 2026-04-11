using UTB.Minute.Db;
using UTB.Minute.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Registrace skupin endpointů
app.MapMealEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

app.Run();
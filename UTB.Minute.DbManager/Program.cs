using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    // Vytvoření základních dat
    var meal1 = new Meal { Id = Guid.NewGuid(), Name = "Svíčková na smetaně", Description = "Hovězí maso, knedlík", Price = 145 };
    var meal2 = new Meal { Id = Guid.NewGuid(), Name = "Smažený sýr", Description = "Hranolky, tatarka", Price = 130 };
    db.Meals.AddRange(meal1, meal2);

    var menuItem = new MenuItem { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Now), AvailablePortions = 10, MealId = meal1.Id };
    db.MenuItems.Add(menuItem);

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
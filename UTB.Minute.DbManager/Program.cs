using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

var builder = WebApplication.CreateBuilder(args);

// Registrace služeb
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

var app = builder.Build();

app.MapDefaultEndpoints();

// ENDPOINT PRO SEEDOVÁNÍ DAT
app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    // 1. Vytvoření jídel
    var m1 = new Meal { Id = Guid.NewGuid(), Name = "Svíčková na smetaně", Description = "Hovězí maso, houskový knedlík, brusinky", Price = 145 };
    var m2 = new Meal { Id = Guid.NewGuid(), Name = "Kuřecí řízek", Description = "Smažený řízek, bramborová kaše, okurka", Price = 135 };
    var m3 = new Meal { Id = Guid.NewGuid(), Name = "Boloňské špagety", Description = "Mleté maso, rajčatová omáčka, sýr", Price = 125 };
    var m4 = new Meal { Id = Guid.NewGuid(), Name = "Čočka na kyselo", Description = "Čočka, uzené maso, sázené vejce, chléb", Price = 110 };

    db.Meals.AddRange(m1, m2, m3, m4);

    // 2. Vytvoření položek v dnešním menu
    var dnes = DateOnly.FromDateTime(DateTime.Now);
    db.MenuItems.AddRange(
        new MenuItem { Id = Guid.NewGuid(), Date = dnes, AvailablePortions = 10, MealId = m1.Id, RowVersion = Array.Empty<byte>() },
        new MenuItem { Id = Guid.NewGuid(), Date = dnes, AvailablePortions = 5, MealId = m2.Id, RowVersion = Array.Empty<byte>() },
        new MenuItem { Id = Guid.NewGuid(), Date = dnes, AvailablePortions = 15, MealId = m3.Id, RowVersion = Array.Empty<byte>() },
        new MenuItem { Id = Guid.NewGuid(), Date = dnes, AvailablePortions = 8, MealId = m4.Id, RowVersion = Array.Empty<byte>() }
    );

    await db.SaveChangesAsync();
    return TypedResults.NoContent();
});

app.Run();
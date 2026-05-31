// --- POVOLENÍ DETAILNÍCH CHYB PRO OIDC/DB DEBUGGING (Na úplném začátku) ---
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Důležité pro .Include() a .ToListAsync()
using UTB.Minute.Db.Entities; // Pro přístup k entitám Meal, MenuItem, Order

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// 1. REGISTRACE POSTGRESQL DATABÁZE PŘES .NET ASPIRE
builder.AddNpgsqlDbContext<AppDbContext>("CanteenDb");

builder.Services.AddSingleton<SseService>();

var app = builder.Build();

var sse = app.Services.GetRequiredService<SseService>();

// --- 2. ENDPOINT: NAČTENÍ MENU Z DATABÁZE ---
app.MapGet("/menu", async (AppDbContext db) =>
{
    var dbMenu = await db.MenuItems
        .Include(m => m.Meal)
        .Select(m => new MenuItemDto(
            m.Id,
            m.Date,
            m.AvailablePortions,
            m.Meal != null ? new MealDto(m.Meal.Id, m.Meal.Name, m.Meal.Description, m.Meal.Price, m.Meal.IsActive) : null
        ))
        .ToListAsync();

    return Results.Ok(dbMenu);
});

// --- 3. ENDPOINT: NAČTENÍ OBJEDNÁVEK Z DATABÁZE ---
app.MapGet("/orders", async (AppDbContext db) =>
{
    var dbOrders = await db.Orders
        .Select(o => new OrderDto(
            o.Id,
            o.MenuItemId,
            o.StudentId,
            (OrderStateDto)o.State // 🚀 OPRAVA: Přetypování databázového enumu (OrderState) na kontraktní (OrderStateDto)
        ))
        .ToListAsync();

    return Results.Ok(dbOrders);
});

// --- 4. ENDPOINT: VYTVOŘENÍ OBJEDNÁVKY V DATABÁZI ---
app.MapPost("/orders", async (CreateOrderDto dto, AppDbContext db, SseService sseService) =>
{
    var menuItem = await db.MenuItems.FindAsync(dto.MenuItemId);
    if (menuItem == null)
    {
        return Results.NotFound("Položka menu neexistuje.");
    }

    if (menuItem.AvailablePortions <= 0)
    {
        return Results.BadRequest("Jídlo je již vyprodané.");
    }

    menuItem.AvailablePortions--;

    var id = Guid.NewGuid();

    var orderEntity = new Order
    {
        Id = id,
        MenuItemId = dto.MenuItemId,
        StudentId = dto.StudentId,
        State = (UTB.Minute.Db.Enums.OrderState)OrderStateDto.Preparing // 🚀 OPRAVA: Přetypování na databázový enum OrderState
    };

    db.Orders.Add(orderEntity);
    await db.SaveChangesAsync();

    var orderDto = new OrderDto(
        orderEntity.Id,
        orderEntity.MenuItemId,
        orderEntity.StudentId,
        OrderStateDto.Preparing
    );

    sseService.Broadcast(orderDto);

    return Results.Created($"/orders/{id}", orderDto);
});

// --- 5. ENDPOINT: ZMĚNA STAVU OBJEDNÁVKY V DATABÁZI ---
app.MapPatch("/orders/{id}/state", async (Guid id, ChangeOrderStateDto change, AppDbContext db, SseService sseService) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order == null)
    {
        return Results.NotFound();
    }

    // 🚀 OPRAVA: Přetypování na databázový enum OrderState při zápisu do databáze
    order.State = (UTB.Minute.Db.Enums.OrderState)change.NewState;
    await db.SaveChangesAsync();

    var orderDto = new OrderDto(
        order.Id,
        order.MenuItemId,
        order.StudentId,
        (OrderStateDto)order.State // 🚀 OPRAVA: Přetypování zpět na kontraktní OrderStateDto pro klienta
    );

    sseService.Broadcast(orderDto);

    return Results.Ok(orderDto);
});

// --- 6. ENDPOINT: ODBĚR SSE NOTIFIKACÍ ---
app.MapGet("/orders/sse", async (HttpContext ctx) =>
{
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["Content-Type"] = "text/event-stream";
    await sse.AddClientAsync(ctx.Response);
});

app.Run();

// --- SSE SLUŽBA PRO BROADCASTING ZMĚN ---
public class SseService
{
    private readonly List<HttpResponse> clients = new();
    private readonly object sync = new();

    public async Task AddClientAsync(HttpResponse response)
    {
        lock (sync) clients.Add(response);
        try
        {
            await response.WriteAsync("data: connected\n\n");
            await response.Body.FlushAsync();

            var tcs = new TaskCompletionSource<object>();
            await tcs.Task;
        }
        catch
        {
            lock (sync) clients.Remove(response);
        }
    }

    public void Broadcast(OrderDto order)
    {
        lock (sync)
        {
            foreach (var client in clients.ToArray())
            {
                try
                {
                    _ = client.WriteAsync($"data: updated\n\n");
                }
                catch
                {
                    clients.Remove(client);
                }
            }
        }
    }
}

public partial class Program { }
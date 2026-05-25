using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.Db.Enums;
using UTB.Minute.WebApi.Services;
using Microsoft.AspNetCore.Http; // Potřebujeme pro práci s Response

namespace UTB.Minute.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");
        group.MapGet("/", GetOrders);
        group.MapPost("/", CreateOrder);
        group.MapPatch("/{id:guid}/state", ChangeOrderState);

        // SSE Endpoint - přidáme metadata pro Swagger/nástroje, i když je nepoužíváme
        group.MapGet("/sse", SubscribeToUpdates).Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
    }

    public static async Task<Ok<OrderDto[]>> GetOrders(AppDbContext db)
    {
        var orders = await db.Orders.Select(o => new OrderDto(o.Id, o.MenuItemId, o.StudentId, (OrderStateDto)o.State)).ToArrayAsync();
        return TypedResults.Ok(orders);
    }

    public static async Task<Results<Created<OrderDto>, NotFound<string>, BadRequest<string>, Conflict<string>>> CreateOrder(CreateOrderDto req, AppDbContext db, SseService? sse = null)
    {
        var menuItem = await db.MenuItems.FindAsync(req.MenuItemId);
        if (menuItem is null) return TypedResults.NotFound("Menu item not found.");
        if (menuItem.AvailablePortions <= 0) return TypedResults.BadRequest("No more portions available.");

        menuItem.AvailablePortions -= 1;
        var order = new Order { Id = Guid.NewGuid(), MenuItemId = req.MenuItemId, StudentId = req.StudentId };
        db.Orders.Add(order);

        try
        {
            await db.SaveChangesAsync();
            var dto = new OrderDto(order.Id, order.MenuItemId, order.StudentId, (OrderStateDto)order.State);
            if (sse != null) await sse.NotifyOrderUpdate(dto);
            return TypedResults.Created($"/orders/{order.Id}", dto);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Conflict("Conflict during order.");
        }
    }

    public static async Task<Results<NoContent, NotFound>> ChangeOrderState(Guid id, ChangeOrderStateDto req, AppDbContext db, SseService? sse = null)
    {
        if (await db.Orders.FindAsync(id) is Order o)
        {
            o.State = (OrderState)req.NewState;
            await db.SaveChangesAsync();
            if (sse != null) await sse.NotifyOrderUpdate(new OrderDto(o.Id, o.MenuItemId, o.StudentId, (OrderStateDto)o.State));
            return TypedResults.NoContent();
        }
        return TypedResults.NotFound();
    }

    // --- OPRAVENÁ METODA PRO SSE ---
    static async Task SubscribeToUpdates(
        SseService sse,
        HttpContext context, // Injekce HTTP kontextu pro nastavení hlaviček
        CancellationToken ct)
    {
        // 1. Nastavení povinných hlaviček pro SSE protokol
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var stream = sse.Subscribe(ct);
        var enumerator = stream.GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                if (!await enumerator.MoveNextAsync()) break;
                var order = enumerator.Current;

                // 2. Ruční zápis do těla odpovědi ve formátu SSE (data: {json}\n\n)
                var json = System.Text.Json.JsonSerializer.Serialize(order);
                await context.Response.WriteAsync($"data: {json}\n\n", ct);

                // 3. Okamžité odeslání (flush) kousku dat ke klientovi
                await context.Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Klient zavřel prohlížeč nebo obnovil stránku - to je v pořádku
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
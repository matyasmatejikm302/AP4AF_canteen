using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.Db.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; // Nezbytné pro [FromServices]

namespace UTB.Minute.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapGet("/", GetOrders);
        group.MapPost("/", CreateOrder);
        group.MapPatch("/{id:guid}/state", ChangeOrderState);

        // Jednoznačné nastavení typu obsahu pro SSE stream
        group.MapGet("/sse", SubscribeToUpdates).Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
    }

    public static async Task<Ok<OrderDto[]>> GetOrders(AppDbContext db)
    {
        var orders = await db.Orders
            .Select(o => new OrderDto(o.Id, o.MenuItemId, o.StudentId, (OrderStateDto)o.State))
            .ToArrayAsync();
        return TypedResults.Ok(orders);
    }

    public static async Task<Results<Created<OrderDto>, NotFound<string>, BadRequest<string>, Conflict<string>>> CreateOrder(
        CreateOrderDto req,
        AppDbContext db,
        [FromServices] UTB.Minute.WebApi.Services.SseService? sse = null) // Použití absolutního jmenného prostoru
    {
        var menuItem = await db.MenuItems.FindAsync(req.MenuItemId);
        if (menuItem is null) return TypedResults.NotFound("Menu item not found.");

        if (menuItem.AvailablePortions <= 0)
            return TypedResults.BadRequest("No more portions available.");

        menuItem.AvailablePortions -= 1;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            MenuItemId = req.MenuItemId,
            StudentId = req.StudentId,
            State = OrderState.Preparing
        };

        db.Orders.Add(order);

        try
        {
            await db.SaveChangesAsync();
            var dto = new OrderDto(order.Id, order.MenuItemId, order.StudentId, (OrderStateDto)order.State);

            if (sse != null)
                await sse.NotifyOrderUpdate(dto);

            return TypedResults.Created($"/orders/{order.Id}", dto);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Conflict("Conflict during order. Please try again.");
        }
    }

    public static async Task<Results<NoContent, NotFound>> ChangeOrderState(
        Guid id,
        ChangeOrderStateDto req,
        AppDbContext db,
        [FromServices] UTB.Minute.WebApi.Services.SseService? sse = null) // Použití absolutního jmenného prostoru
    {
        if (await db.Orders.FindAsync(id) is Order o)
        {
            o.State = (OrderState)req.NewState;
            await db.SaveChangesAsync();

            var dto = new OrderDto(o.Id, o.MenuItemId, o.StudentId, (OrderStateDto)o.State);

            if (sse != null)
                await sse.NotifyOrderUpdate(dto);

            return TypedResults.NoContent();
        }
        return TypedResults.NotFound();
    }

    // Obsluha real-time SSE streamu s absolutním jmenným prostorem pro SseService
    static async Task SubscribeToUpdates(
        [FromServices] UTB.Minute.WebApi.Services.SseService sse, // Použití absolutního jmenného prostoru
        HttpContext context,
        CancellationToken ct)
    {
        // Nastavení hlaviček pro správný SSE protokol
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var stream = sse.Subscribe(ct);
        var enumerator = stream.GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                OrderDto order;
                try
                {
                    if (!await enumerator.MoveNextAsync()) break;
                    order = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    // Uživatel zavřel prohlížeč nebo obnovil stránku
                    break;
                }

                var json = System.Text.Json.JsonSerializer.Serialize(order);
                await context.Response.WriteAsync($"data: {json}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
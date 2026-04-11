using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.Db.Enums;

namespace UTB.Minute.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapGet("/", GetOrders);
        group.MapPost("/", CreateOrder);
        group.MapPatch("/{id:guid}/state", ChangeOrderState);
    }

    public static async Task<Ok<OrderDto[]>> GetOrders(AppDbContext db)
    {
        var orders = await db.Orders
            .Select(o => new OrderDto(o.Id, o.MenuItemId, o.StudentId, (OrderStateDto)o.State))
            .ToArrayAsync();
        return TypedResults.Ok(orders);
    }

    public static async Task<Results<Created<OrderDto>, NotFound<string>, BadRequest<string>, Conflict<string>>> CreateOrder(CreateOrderDto req, AppDbContext db)
    {
        var menuItem = await db.MenuItems.FindAsync(req.MenuItemId);
        if (menuItem is null) return TypedResults.NotFound("Menu item not found.");

        if (menuItem.AvailablePortions <= 0)
            return TypedResults.BadRequest("No more portions available.");

        // Snížíme počet porcí
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
        }
        catch (DbUpdateConcurrencyException)
        {
            // Pokud se dva studenti trefili do stejné milisekundy u poslední porce
            return TypedResults.Conflict("Portion was taken by someone else. Please try again.");
        }

        return TypedResults.Created($"/orders/{order.Id}",
            new OrderDto(order.Id, order.MenuItemId, order.StudentId, (OrderStateDto)order.State));
    }

    public static async Task<Results<NoContent, NotFound>> ChangeOrderState(Guid id, ChangeOrderStateDto req, AppDbContext db)
    {
        if (await db.Orders.FindAsync(id) is Order o)
        {
            o.State = (OrderState)req.NewState;
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        return TypedResults.NotFound();
    }
}
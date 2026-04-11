using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.WebApi.Endpoints;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/menu");

        group.MapGet("/", GetMenuItems);
        group.MapPost("/", CreateMenuItem);
        group.MapPut("/{id:guid}", UpdateMenuItem);
        group.MapDelete("/{id:guid}", DeleteMenuItem);
    }

    public static async Task<Ok<MenuItemDto[]>> GetMenuItems(AppDbContext db)
    {
        var items = await db.MenuItems
            .Include(mi => mi.Meal)
            .Select(mi => new MenuItemDto(
                mi.Id,
                mi.Date,
                mi.AvailablePortions,
                new MealDto(mi.Meal.Id, mi.Meal.Name, mi.Meal.Description, mi.Meal.Price, mi.Meal.IsActive)))
            .ToArrayAsync();

        return TypedResults.Ok(items);
    }

    public static async Task<Results<Created<MenuItemDto>, NotFound<string>>> CreateMenuItem(CreateMenuItemDto req, AppDbContext db)
    {
        var meal = await db.Meals.FindAsync(req.MealId);
        if (meal is null) return TypedResults.NotFound("Meal not found.");

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            Date = req.Date,
            AvailablePortions = req.AvailablePortions,
            MealId = req.MealId
        };

        db.MenuItems.Add(menuItem);
        await db.SaveChangesAsync();

        var dto = new MenuItemDto(menuItem.Id, menuItem.Date, menuItem.AvailablePortions,
            new MealDto(meal.Id, meal.Name, meal.Description, meal.Price, meal.IsActive));

        return TypedResults.Created($"/menu/{menuItem.Id}", dto);
    }

    public static async Task<Results<NoContent, NotFound>> UpdateMenuItem(Guid id, UpdateMenuItemDto req, AppDbContext db)
    {
        if (await db.MenuItems.FindAsync(id) is MenuItem mi)
        {
            mi.Date = req.Date;
            mi.AvailablePortions = req.AvailablePortions;
            mi.MealId = req.MealId;
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        return TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteMenuItem(Guid id, AppDbContext db)
    {
        if (await db.MenuItems.FindAsync(id) is MenuItem mi)
        {
            db.MenuItems.Remove(mi);
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        return TypedResults.NotFound();
    }
}
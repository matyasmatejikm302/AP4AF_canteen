using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.WebApi.Endpoints;

public static class MealEndpoints
{
    public static void MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meals");
        group.MapGet("/", GetMeals);
        group.MapPost("/", CreateMeal);
        group.MapPatch("/{id:guid}/deactivate", DeactivateMeal);
    }

    public static async Task<Ok<MealDto[]>> GetMeals(AppDbContext db)
    {
        var meals = await db.Meals.Select(m => new MealDto(m.Id, m.Name, m.Description, m.Price, m.IsActive)).ToArrayAsync();
        return TypedResults.Ok(meals);
    }

    public static async Task<Created<MealDto>> CreateMeal(CreateMealDto req, AppDbContext db)
    {
        var meal = new Meal { Id = Guid.NewGuid(), Name = req.Name, Description = req.Description, Price = req.Price };
        db.Meals.Add(meal);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/meals/{meal.Id}", new MealDto(meal.Id, meal.Name, meal.Description, meal.Price, meal.IsActive));
    }

    public static async Task<Results<NoContent, NotFound>> DeactivateMeal(Guid id, AppDbContext db)
    {
        if (await db.Meals.FindAsync(id) is Meal m) { m.IsActive = false; await db.SaveChangesAsync(); return TypedResults.NoContent(); }
        return TypedResults.NotFound();
    }
}
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.WebApi.Endpoints;
using Xunit;

namespace UTB.Minute.WebApi.Tests;

public class CanteenTests
{
    // Pomocná metoda pro vytvoření čisté in-memory DB pro každý test
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateMeal_SavesToDatabase()
    {
        var db = GetDbContext();
        var req = new CreateMealDto("Test Jídlo", "Popis", 100);

        var result = await MealEndpoints.CreateMeal(req, db);

        Assert.IsType<Created<MealDto>>(result);
        Assert.Equal(1, await db.Meals.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_DecreasesPortions_And_ReturnsCreated()
    {
        var db = GetDbContext();
        // Příprava: jídlo a položka v menu s 5 porcemi
        var meal = new Meal { Id = Guid.NewGuid(), Name = "Test", Description = "-", Price = 10 };
        db.Meals.Add(meal);
        var menu = new MenuItem { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Now), AvailablePortions = 5, MealId = meal.Id, RowVersion = Array.Empty<byte>() };
        db.MenuItems.Add(menu);
        await db.SaveChangesAsync();

        var req = new CreateOrderDto(menu.Id, "student1");
        var result = await OrderEndpoints.CreateOrder(req, db);

        Assert.IsType<Created<OrderDto>>(result.Result);
        var updatedMenu = await db.MenuItems.FindAsync(menu.Id);
        Assert.Equal(4, updatedMenu!.AvailablePortions); // Porce musí klesnout
    }

    [Fact]
    public async Task CreateOrder_WhenNoPortions_ReturnsBadRequest()
    {
        var db = GetDbContext();
        var menu = new MenuItem { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Now), AvailablePortions = 0, MealId = Guid.NewGuid(), RowVersion = Array.Empty<byte>() };
        db.MenuItems.Add(menu);
        await db.SaveChangesAsync();

        var req = new CreateOrderDto(menu.Id, "student1");
        var result = await OrderEndpoints.CreateOrder(req, db);

        Assert.IsType<BadRequest<string>>(result.Result);
    }
}
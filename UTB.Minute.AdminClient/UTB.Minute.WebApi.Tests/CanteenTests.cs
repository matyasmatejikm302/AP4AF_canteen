using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using UTB.Minute.Contracts;
using Xunit;

namespace UTB.Minute.WebApi.Tests;

public class CanteenTests
{
    [Fact]
    public async Task CreateMeal_SavesToDatabase()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<global::UTB.Minute.AppHost.Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("webapi");
        var req = new CreateMealDto("Test Jídlo", "Popis", 100);

        var response = await httpClient.PostAsJsonAsync("/meals", req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var getResponse = await httpClient.GetAsync("/meals");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var meals = await getResponse.Content.ReadFromJsonAsync<List<MealDto>>();
        Assert.NotNull(meals);
        Assert.Contains(meals, m => m.Name == "Test Jídlo");
    }

    [Fact]
    public async Task CreateOrder_DecreasesPortions_And_ReturnsCreated()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<global::UTB.Minute.AppHost.Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("webapi");

        var mealReq = new CreateMealDto("Test Jídlo Pro Objednávku", "Popis", 120);
        var mealRes = await httpClient.PostAsJsonAsync("/meals", mealReq);
        Assert.Equal(HttpStatusCode.Created, mealRes.StatusCode);
        var createdMeal = await mealRes.Content.ReadFromJsonAsync<MealDto>();
        Assert.NotNull(createdMeal);

        var menuReq = new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Now), 5, createdMeal.Id);
        var menuRes = await httpClient.PostAsJsonAsync("/menu", menuReq);
        Assert.Equal(HttpStatusCode.Created, menuRes.StatusCode);
        var createdMenu = await menuRes.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.NotNull(createdMenu);

        var orderReq = new CreateOrderDto(createdMenu.Id, "student1");

        var orderRes = await httpClient.PostAsJsonAsync("/orders", orderReq);

        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);

        var getMenuRes = await httpClient.GetAsync("/menu");
        Assert.Equal(HttpStatusCode.OK, getMenuRes.StatusCode);

        var menuItems = await getMenuRes.Content.ReadFromJsonAsync<List<MenuItemDto>>();
        Assert.NotNull(menuItems);

        var updatedMenu = menuItems.FirstOrDefault(m => m.Id == createdMenu.Id);
        Assert.NotNull(updatedMenu);
        Assert.Equal(4, updatedMenu.AvailablePortions);
    }

    [Fact]
    public async Task CreateOrder_WhenNoPortions_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<global::UTB.Minute.AppHost.Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("webapi");

        var mealReq = new CreateMealDto("Jídlo Bez Porcí", "Popis", 120);
        var mealRes = await httpClient.PostAsJsonAsync("/meals", mealReq);
        var createdMeal = await mealRes.Content.ReadFromJsonAsync<MealDto>();
        Assert.NotNull(createdMeal);

        var menuReq = new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Now), 0, createdMeal.Id);
        var menuRes = await httpClient.PostAsJsonAsync("/menu", menuReq);
        var createdMenu = await menuRes.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.NotNull(createdMenu);

        var orderReq = new CreateOrderDto(createdMenu.Id, "student1");

        var orderRes = await httpClient.PostAsJsonAsync("/orders", orderReq);

        Assert.Equal(HttpStatusCode.BadRequest, orderRes.StatusCode);
    }
}
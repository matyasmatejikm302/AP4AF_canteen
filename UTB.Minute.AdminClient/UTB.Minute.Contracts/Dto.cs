namespace UTB.Minute.Contracts;

public enum OrderStateDto { Preparing, Ready, Cancelled, Completed }

public record MealDto(Guid Id, string Name, string Description, decimal Price, bool IsActive);
public record CreateMealDto(string Name, string Description, decimal Price);
public record UpdateMealDto(string Name, string Description, decimal Price);

public record MenuItemDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int AvailablePortions { get; set; }
    public MealDto Meal { get; set; } = null!;

    public MenuItemDto() { }
    public MenuItemDto(Guid id, DateOnly date, int portions, MealDto meal)
    {
        Id = id; Date = date; AvailablePortions = portions; Meal = meal;
    }
}

public record CreateMenuItemDto(DateOnly Date, int AvailablePortions, Guid MealId);
public record UpdateMenuItemDto(DateOnly Date, int AvailablePortions, Guid MealId);

public record OrderDto(Guid Id, Guid MenuItemId, string StudentId, OrderStateDto State)
{
    public OrderStateDto State { get; set; } = State;
}
public record CreateOrderDto(Guid MenuItemId, string StudentId);
public record ChangeOrderStateDto(OrderStateDto NewState);
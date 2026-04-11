namespace UTB.Minute.Contracts;

public enum OrderStateDto { Preparing, Ready, Cancelled, Completed }

public record MealDto(Guid Id, string Name, string Description, decimal Price, bool IsActive);
public record CreateMealDto(string Name, string Description, decimal Price);
public record UpdateMealDto(string Name, string Description, decimal Price);

public record MenuItemDto(Guid Id, DateOnly Date, int AvailablePortions, MealDto Meal);
public record CreateMenuItemDto(DateOnly Date, int AvailablePortions, Guid MealId);
public record UpdateMenuItemDto(DateOnly Date, int AvailablePortions, Guid MealId);

public record OrderDto(Guid Id, Guid MenuItemId, string StudentId, OrderStateDto State);
public record CreateOrderDto(Guid MenuItemId, string StudentId);
public record ChangeOrderStateDto(OrderStateDto NewState);
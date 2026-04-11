using UTB.Minute.Db.Enums;

namespace UTB.Minute.Db.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public required string StudentId { get; set; }
    public OrderState State { get; set; } = OrderState.Preparing;
}
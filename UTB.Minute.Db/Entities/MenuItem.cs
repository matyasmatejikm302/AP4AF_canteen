using System.ComponentModel.DataAnnotations;

namespace UTB.Minute.Db.Entities;

public class MenuItem
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int AvailablePortions { get; set; }

    public Guid MealId { get; set; }
    public Meal Meal { get; set; } = null!;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
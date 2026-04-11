namespace UTB.Minute.Db.Entities;

public class Meal
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
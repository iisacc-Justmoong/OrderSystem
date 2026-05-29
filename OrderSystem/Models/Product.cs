namespace OrderSystem.Models;

public sealed class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Inventory? Inventory { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];
}

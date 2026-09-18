namespace OrderSystem.Models;

public sealed class Inventory
{
    public long ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

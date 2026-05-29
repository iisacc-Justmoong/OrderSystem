namespace OrderSystem.Models;

public sealed class Product
{
    public long Id { get; set; }

    public required string Sku { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Inventory? Inventory { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];

    public List<InventoryTransaction> InventoryTransactions { get; set; } = [];
}

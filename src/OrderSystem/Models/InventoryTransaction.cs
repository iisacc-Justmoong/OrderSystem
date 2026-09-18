namespace OrderSystem.Models;

public sealed class InventoryTransaction
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public Product? Product { get; set; }

    public int QuantityChange { get; set; }

    public InventoryTransactionReason Reason { get; set; }

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

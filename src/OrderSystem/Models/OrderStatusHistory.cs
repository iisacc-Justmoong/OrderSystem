namespace OrderSystem.Models;

public sealed class OrderStatusHistory
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public Order? Order { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

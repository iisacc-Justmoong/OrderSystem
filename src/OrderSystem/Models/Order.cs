namespace OrderSystem.Models;

public sealed class Order
{
    public long Id { get; set; }

    public required string OrderNumber { get; set; }

    public long CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];

    public List<OrderStatusHistory> StatusHistories { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];
}

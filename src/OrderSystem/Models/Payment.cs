namespace OrderSystem.Models;

public sealed class Payment
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public Order? Order { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using OrderSystem.Models;

namespace OrderSystem.DTOs;

public sealed record PaymentResponse(
    long Id,
    long OrderId,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    decimal Amount,
    DateTime? PaidAt,
    DateTime CreatedAt);

using OrderSystem.Models;

namespace OrderSystem.DTOs;

public sealed record OrderResponse(
    int Id,
    int CustomerId,
    string? CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    int ProductId,
    string? ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

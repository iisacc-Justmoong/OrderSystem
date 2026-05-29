using OrderSystem.Models;

namespace OrderSystem.DTOs;

public sealed record OrderResponse(
    long Id,
    string OrderNumber,
    long CustomerId,
    string? CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    long ProductId,
    string? ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

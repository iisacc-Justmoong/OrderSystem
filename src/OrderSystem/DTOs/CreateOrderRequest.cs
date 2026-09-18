using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record CreateOrderRequest(
    [Range(1, long.MaxValue)] long CustomerId,
    [MinLength(1)] IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    [Range(1, long.MaxValue)] long ProductId,
    [Range(1, int.MaxValue)] int Quantity);

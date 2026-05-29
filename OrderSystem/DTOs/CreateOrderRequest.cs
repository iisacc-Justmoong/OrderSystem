using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record CreateOrderRequest(
    [Range(1, int.MaxValue)] int CustomerId,
    [MinLength(1)] IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, int.MaxValue)] int Quantity);

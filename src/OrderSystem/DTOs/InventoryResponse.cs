namespace OrderSystem.DTOs;

public sealed record InventoryResponse(
    long ProductId,
    string? Sku,
    string? ProductName,
    int Quantity,
    DateTime UpdatedAt);

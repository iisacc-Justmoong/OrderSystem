namespace OrderSystem.DTOs;

public sealed record ProductResponse(
    long Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    bool IsActive,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

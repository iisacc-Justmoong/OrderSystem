namespace OrderSystem.DTOs;

public sealed record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    bool IsActive,
    int StockQuantity,
    DateTime CreatedAt);

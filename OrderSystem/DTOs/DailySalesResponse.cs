namespace OrderSystem.DTOs;

public sealed record DailySalesResponse(
    DateOnly Date,
    int OrderCount,
    int ItemCount,
    decimal GrossSales);

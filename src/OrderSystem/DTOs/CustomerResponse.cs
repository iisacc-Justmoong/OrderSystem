namespace OrderSystem.DTOs;

public sealed record CustomerResponse(
    long Id,
    string Name,
    string Email,
    string? Phone,
    DateTime CreatedAt);

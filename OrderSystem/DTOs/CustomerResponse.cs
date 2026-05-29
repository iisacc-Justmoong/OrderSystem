namespace OrderSystem.DTOs;

public sealed record CustomerResponse(
    int Id,
    string Name,
    string Email,
    string? Phone,
    DateTime CreatedAt);

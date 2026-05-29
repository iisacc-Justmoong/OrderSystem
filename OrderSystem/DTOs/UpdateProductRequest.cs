using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record UpdateProductRequest(
    [Required, StringLength(64)] string Sku,
    [Required, StringLength(200)] string Name,
    [StringLength(1000)] string? Description,
    [Range(typeof(decimal), "0.01", "999999999")] decimal Price,
    bool IsActive);

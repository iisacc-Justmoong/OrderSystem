using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record AdjustInventoryRequest(
    [Range(-2147483648, 2147483647)] int QuantityChange,
    [StringLength(50)] string? ReferenceType = null,
    long? ReferenceId = null);

using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record CreateCustomerRequest(
    [Required, StringLength(120)] string Name,
    [Required, EmailAddress, StringLength(240)] string Email,
    [StringLength(40)] string? Phone);

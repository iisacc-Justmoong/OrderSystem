using System.ComponentModel.DataAnnotations;

namespace OrderSystem.DTOs;

public sealed record CreateCustomerRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(255)] string Email,
    [StringLength(50)] string? Phone);

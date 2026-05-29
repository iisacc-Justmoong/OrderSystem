using System.ComponentModel.DataAnnotations;
using OrderSystem.Models;

namespace OrderSystem.DTOs;

public sealed record CreatePaymentRequest(
    [Required] PaymentMethod PaymentMethod,
    [Required] PaymentStatus Status,
    [Range(typeof(decimal), "0.01", "999999999")] decimal Amount);

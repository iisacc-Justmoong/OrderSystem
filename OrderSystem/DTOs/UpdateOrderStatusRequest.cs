using System.ComponentModel.DataAnnotations;
using OrderSystem.Models;

namespace OrderSystem.DTOs;

public sealed record UpdateOrderStatusRequest(
    [Required] OrderStatus Status);

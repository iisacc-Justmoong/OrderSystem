using Microsoft.AspNetCore.Mvc;
using OrderSystem.DTOs;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(OrderService orderService) : ControllerBase
{
    private readonly OrderService _orderService = orderService;

    [HttpGet("daily-sales")]
    public async Task<ActionResult<IReadOnlyList<DailySalesResponse>>> DailySales(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _orderService.GetDailySalesAsync(from, to, cancellationToken);
            return Ok(report);
        }
        catch (DomainException exception)
        {
            return this.ToActionResult<IReadOnlyList<DailySalesResponse>>(exception);
        }
    }
}

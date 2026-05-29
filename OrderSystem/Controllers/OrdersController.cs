using Microsoft.AspNetCore.Mvc;
using OrderSystem.DTOs;
using OrderSystem.Models;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    private readonly OrderService _orderService = orderService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> List(
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.ListOrdersAsync(status, from, to, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderAsync(id, cancellationToken);
        return order is null
            ? NotFound()
            : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
        }
        catch (DomainException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        int id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.UpdateStatusAsync(id, request.Status, cancellationToken);
            return Ok(order);
        }
        catch (DomainException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.CancelOrderAsync(id, cancellationToken);
            return Ok(order);
        }
        catch (DomainException exception)
        {
            return ToProblem(exception);
        }
    }

    private ObjectResult ToProblem(DomainException exception)
    {
        return Problem(statusCode: exception.StatusCode, title: exception.Message);
    }
}

using Microsoft.AspNetCore.Mvc;
using OrderSystem.DTOs;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(InventoryService inventoryService) : ControllerBase
{
    private readonly InventoryService _inventoryService = inventoryService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryResponse>>> List(CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.ListAsync(cancellationToken);
        return Ok(inventory);
    }

    [HttpGet("{productId:long}")]
    public async Task<ActionResult<InventoryResponse>> Get(long productId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetAsync(productId, cancellationToken);
        return inventory is null
            ? NotFound()
            : Ok(inventory);
    }

    [HttpPost("{productId:long}/adjust")]
    public async Task<ActionResult<InventoryResponse>> Adjust(
        long productId,
        AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var inventory = await _inventoryService.AdjustAsync(productId, request, cancellationToken);
            return Ok(inventory);
        }
        catch (DomainException exception)
        {
            return Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}

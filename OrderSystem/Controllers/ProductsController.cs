using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(AppDbContext dbContext) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .Include(product => product.Inventory)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

        return Ok(products.Select(product => product.ToResponse()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductResponse>> Get(long id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

        return product is null
            ? NotFound()
            : Ok(product.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsActive = request.IsActive,
            CreatedAt = now,
            Inventory = new Inventory
            {
                Quantity = request.InitialStockQuantity,
                UpdatedAt = now
            }
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.InitialStockQuantity > 0)
        {
            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                QuantityChange = request.InitialStockQuantity,
                Reason = InventoryTransactionReason.StockReceived,
                ReferenceType = "Product",
                ReferenceId = product.Id,
                CreatedAt = now
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var response = product.ToResponse();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, response);
    }
}

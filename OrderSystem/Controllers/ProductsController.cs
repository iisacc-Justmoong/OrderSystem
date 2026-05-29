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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken cancellationToken)
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
        var now = DateTime.UtcNow;
        var product = new Product
        {
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

        var response = product.ToResponse();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, response);
    }
}

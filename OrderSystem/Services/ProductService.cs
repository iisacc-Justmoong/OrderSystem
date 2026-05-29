using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;

namespace OrderSystem.Services;

public sealed class ProductService(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .Include(product => product.Inventory)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

        return products.Select(product => product.ToResponse()).ToList();
    }

    public async Task<ProductResponse?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

        return product?.ToResponse();
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
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

        return product.ToResponse();
    }

    public async Task<ProductResponse> UpdateAsync(
        long id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            throw new DomainException($"Product {id} was not found.", 404);
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }
}

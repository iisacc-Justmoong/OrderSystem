using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;

namespace OrderSystem.Services;

public sealed class InventoryService(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<InventoryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var inventory = await _dbContext.Inventories
            .Include(item => item.Product)
            .OrderBy(item => item.ProductId)
            .ToListAsync(cancellationToken);

        return inventory.Select(ToResponse).ToList();
    }

    public async Task<InventoryResponse?> GetAsync(long productId, CancellationToken cancellationToken = default)
    {
        var inventory = await _dbContext.Inventories
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        return inventory is null ? null : ToResponse(inventory);
    }

    public async Task<InventoryResponse> AdjustAsync(
        long productId,
        AdjustInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantityChange == 0)
        {
            throw new DomainException("Inventory adjustment quantity must not be zero.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var inventory = await _dbContext.Inventories
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);
        if (inventory is null)
        {
            throw new DomainException($"Inventory for product {productId} was not found.", 404);
        }

        var adjustedQuantity = inventory.Quantity + request.QuantityChange;
        if (adjustedQuantity < 0)
        {
            throw new DomainException("Inventory adjustment would make stock negative.", 409);
        }

        var now = DateTime.UtcNow;
        inventory.Quantity = adjustedQuantity;
        inventory.UpdatedAt = now;

        _dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = productId,
            QuantityChange = request.QuantityChange,
            Reason = InventoryTransactionReason.ManualAdjustment,
            ReferenceType = request.ReferenceType ?? "InventoryAdjustment",
            ReferenceId = request.ReferenceId,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResponse(inventory);
    }

    private static InventoryResponse ToResponse(Inventory inventory)
    {
        return new InventoryResponse(
            inventory.ProductId,
            inventory.Product?.Sku,
            inventory.Product?.Name,
            inventory.Quantity,
            inventory.UpdatedAt);
    }
}

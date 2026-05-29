using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;

namespace OrderSystem.Services;

public sealed class OrderService(AppDbContext dbContext)
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed],
            [OrderStatus.Confirmed] = [OrderStatus.Preparing],
            [OrderStatus.Preparing] = [OrderStatus.Shipped],
            [OrderStatus.Shipped] = [OrderStatus.Delivered],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = []
        };

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new DomainException("Order must include at least one item.");
        }

        var requestedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new CreateOrderItemRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        if (requestedItems.Any(item => item.Quantity <= 0))
        {
            throw new DomainException("Order item quantity must be greater than zero.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var customer = await _dbContext.Customers.FindAsync([request.CustomerId], cancellationToken);
        if (customer is null)
        {
            throw new DomainException($"Customer {request.CustomerId} was not found.", 404);
        }

        var productIds = requestedItems.Select(item => item.ProductId).ToArray();
        var products = await _dbContext.Products
            .Include(product => product.Inventory)
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var productId in productIds.Except(products.Keys))
        {
            throw new DomainException($"Product {productId} was not found.", 404);
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            CustomerId = customer.Id,
            Customer = customer,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var requestedItem in requestedItems)
        {
            var product = products[requestedItem.ProductId];
            if (!product.IsActive)
            {
                throw new DomainException($"Product {product.Id} is not active.");
            }

            if (product.Inventory is null)
            {
                throw new DomainException($"Product {product.Id} has no inventory record.");
            }

            if (product.Inventory.Quantity < requestedItem.Quantity)
            {
                throw new DomainException($"Product {product.Id} does not have enough stock.");
            }

            product.Inventory.Quantity -= requestedItem.Quantity;
            product.Inventory.UpdatedAt = now;

            var lineTotal = product.Price * requestedItem.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });
            order.TotalAmount += lineTotal;
        }

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order.ToResponse();
    }

    public async Task<OrderResponse?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await IncludeOrderGraph(_dbContext.Orders)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

        return order?.ToResponse();
    }

    public async Task<IReadOnlyList<OrderResponse>> ListOrdersAsync(
        OrderStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = IncludeOrderGraph(_dbContext.Orders).AsQueryable();

        if (status is not null)
        {
            query = query.Where(order => order.Status == status);
        }

        if (from is not null)
        {
            query = query.Where(order => order.CreatedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(order => order.CreatedAt <= to.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(order => order.ToResponse()).ToList();
    }

    public async Task<OrderResponse> UpdateStatusAsync(
        int id,
        OrderStatus nextStatus,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await IncludeOrderGraph(_dbContext.Orders)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (order is null)
        {
            throw new DomainException($"Order {id} was not found.", 404);
        }

        if (!CanTransition(order.Status, nextStatus))
        {
            throw new DomainException($"Order cannot move from {order.Status} to {nextStatus}.");
        }

        order.Status = nextStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order.ToResponse();
    }

    public async Task<OrderResponse> CancelOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await IncludeOrderGraph(_dbContext.Orders)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (order is null)
        {
            throw new DomainException($"Order {id} was not found.", 404);
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            throw new DomainException($"Order {id} cannot be cancelled after {order.Status}.");
        }

        var productIds = order.Items.Select(item => item.ProductId).ToArray();
        var inventories = await _dbContext.Inventories
            .Where(inventory => productIds.Contains(inventory.ProductId))
            .ToDictionaryAsync(inventory => inventory.ProductId, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in order.Items)
        {
            if (!inventories.TryGetValue(item.ProductId, out var inventory))
            {
                throw new DomainException($"Product {item.ProductId} has no inventory record.");
            }

            inventory.Quantity += item.Quantity;
            inventory.UpdatedAt = now;
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order.ToResponse();
    }

    public async Task<IReadOnlyList<DailySalesResponse>> GetDailySalesAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new DomainException("The report end date must be greater than or equal to the start date.");
        }

        var fromDateTime = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateTime = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var orders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order => order.Status != OrderStatus.Cancelled)
            .Where(order => order.CreatedAt >= fromDateTime && order.CreatedAt < toDateTime)
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(order => DateOnly.FromDateTime(order.CreatedAt))
            .OrderBy(group => group.Key)
            .Select(group => new DailySalesResponse(
                group.Key,
                group.Count(),
                group.SelectMany(order => order.Items).Sum(item => item.Quantity),
                group.Sum(order => order.TotalAmount)))
            .ToList();
    }

    private static bool CanTransition(OrderStatus currentStatus, OrderStatus nextStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowedStatuses)
            && allowedStatuses.Contains(nextStatus);
    }

    private static IQueryable<Order> IncludeOrderGraph(IQueryable<Order> orders)
    {
        return orders
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product);
    }
}

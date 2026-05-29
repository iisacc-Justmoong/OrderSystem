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
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var productId in productIds.Except(products.Keys))
        {
            throw new DomainException($"Product {productId} was not found.", 404);
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            OrderNumber = CreateOrderNumber(now),
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

            var updatedRows = await _dbContext.Inventories
                .Where(inventory => inventory.ProductId == product.Id
                                    && inventory.Quantity >= requestedItem.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(inventory => inventory.Quantity, inventory => inventory.Quantity - requestedItem.Quantity)
                    .SetProperty(inventory => inventory.UpdatedAt, now), cancellationToken);
            if (updatedRows == 0)
            {
                throw new DomainException($"Product {product.Id} does not have enough stock.");
            }

            var lineTotal = product.Price * requestedItem.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });
            order.TotalAmount += lineTotal;
        }

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = null,
            ToStatus = OrderStatus.Pending,
            Reason = "Order created",
            CreatedAt = now
        });

        foreach (var item in order.Items)
        {
            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                QuantityChange = -item.Quantity,
                Reason = InventoryTransactionReason.OrderCreated,
                ReferenceType = "Order",
                ReferenceId = order.Id,
                CreatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredOrderResponseAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse?> GetOrderAsync(long id, CancellationToken cancellationToken = default)
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
        long id,
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

        var previousStatus = order.Status;
        if (!CanTransition(previousStatus, nextStatus))
        {
            throw new DomainException($"Order cannot move from {order.Status} to {nextStatus}.");
        }

        order.Status = nextStatus;
        var now = DateTime.UtcNow;
        order.UpdatedAt = now;

        _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = nextStatus,
            Reason = "Status updated",
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredOrderResponseAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse> CancelOrderAsync(long id, CancellationToken cancellationToken = default)
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

        var now = DateTime.UtcNow;
        foreach (var item in order.Items)
        {
            var updatedRows = await _dbContext.Inventories
                .Where(inventory => inventory.ProductId == item.ProductId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(inventory => inventory.Quantity, inventory => inventory.Quantity + item.Quantity)
                    .SetProperty(inventory => inventory.UpdatedAt, now), cancellationToken);
            if (updatedRows == 0)
            {
                throw new DomainException($"Product {item.ProductId} has no inventory record.");
            }

            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                QuantityChange = item.Quantity,
                Reason = InventoryTransactionReason.OrderCancelled,
                ReferenceType = "Order",
                ReferenceId = order.Id,
                CreatedAt = now
            });
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = now;

        _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Cancelled,
            Reason = "Order cancelled",
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredOrderResponseAsync(order.Id, cancellationToken);
    }

    public async Task<PaymentResponse> RecordPaymentAsync(
        long orderId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await _dbContext.Orders.FindAsync([orderId], cancellationToken);
        if (order is null)
        {
            throw new DomainException($"Order {orderId} was not found.", 404);
        }

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = request.PaymentMethod,
            Status = request.Status,
            Amount = request.Amount,
            PaidAt = request.Status == PaymentStatus.Paid ? now : null,
            CreatedAt = now
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment.ToResponse();
    }

    public async Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var orderExists = await _dbContext.Orders.AnyAsync(order => order.Id == orderId, cancellationToken);
        if (!orderExists)
        {
            throw new DomainException($"Order {orderId} was not found.", 404);
        }

        var payments = await _dbContext.Payments
            .Where(payment => payment.OrderId == orderId)
            .OrderBy(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(payment => payment.ToResponse()).ToList();
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

    private async Task<OrderResponse> GetRequiredOrderResponseAsync(long id, CancellationToken cancellationToken)
    {
        return await GetOrderAsync(id, cancellationToken)
            ?? throw new DomainException($"Order {id} was not found.", 404);
    }

    private static string CreateOrderNumber(DateTime now)
    {
        return $"ORD-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private static IQueryable<Order> IncludeOrderGraph(IQueryable<Order> orders)
    {
        return orders
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product);
    }
}

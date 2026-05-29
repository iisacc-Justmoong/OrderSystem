using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;
using OrderSystem.Services;
using Xunit;

namespace OrderSystem.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_DeductsInventoryAndStoresServerPriceSnapshot()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 120m, stockQuantity: 10);

        var order = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 2)]));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(240m, order.TotalAmount);
        Assert.Equal(120m, order.Items.Single().UnitPrice);
        Assert.Equal(240m, order.Items.Single().LineTotal);
        Assert.Equal(8, await database.StockQuantityAsync(product.Id));

        product.Price = 150m;
        await database.DbContext.SaveChangesAsync();

        var storedItem = await database.DbContext.OrderItems.SingleAsync();
        Assert.Equal(120m, storedItem.UnitPrice);
    }

    [Fact]
    public async Task CreateOrderAsync_WritesLedgerRecordsAndOrderNumber()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 120m, stockQuantity: 10);

        var order = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 2)]));

        Assert.StartsWith("ORD-", order.OrderNumber);

        var statusHistory = await database.DbContext.OrderStatusHistories.SingleAsync();
        Assert.Null(statusHistory.FromStatus);
        Assert.Equal(OrderStatus.Pending, statusHistory.ToStatus);
        Assert.Equal(order.Id, statusHistory.OrderId);

        var inventoryTransaction = await database.DbContext.InventoryTransactions.SingleAsync();
        Assert.Equal(product.Id, inventoryTransaction.ProductId);
        Assert.Equal(-2, inventoryTransaction.QuantityChange);
        Assert.Equal(InventoryTransactionReason.OrderCreated, inventoryTransaction.Reason);
        Assert.Equal("Order", inventoryTransaction.ReferenceType);
        Assert.Equal(order.Id, inventoryTransaction.ReferenceId);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsInsufficientInventoryWithoutChangingStock()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 85m, stockQuantity: 1);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            database.Service.CreateOrderAsync(new CreateOrderRequest(
                customer.Id,
                [new CreateOrderItemRequest(product.Id, 2)])));

        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await database.DbContext.Orders.CountAsync());
        Assert.Equal(1, await database.StockQuantityAsync(product.Id));
    }

    [Fact]
    public async Task CancelOrderAsync_RestoresInventoryAndMarksOrderCancelled()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 45m, stockQuantity: 6);
        var created = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 4)]));

        var cancelled = await database.Service.CancelOrderAsync(created.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(6, await database.StockQuantityAsync(product.Id));

        var recoveryTransaction = await database.DbContext.InventoryTransactions
            .SingleAsync(transaction => transaction.Reason == InventoryTransactionReason.OrderCancelled);
        Assert.Equal(4, recoveryTransaction.QuantityChange);
        Assert.Equal(created.Id, recoveryTransaction.ReferenceId);

        var cancellationHistory = await database.DbContext.OrderStatusHistories
            .SingleAsync(history => history.ToStatus == OrderStatus.Cancelled);
        Assert.Equal(OrderStatus.Pending, cancellationHistory.FromStatus);
    }

    [Fact]
    public async Task CancelOrderAsync_RejectsShippedOrderAndKeepsInventoryDeducted()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 70m, stockQuantity: 5);
        var created = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 2)]));

        await database.Service.UpdateStatusAsync(created.Id, OrderStatus.Confirmed);
        await database.Service.UpdateStatusAsync(created.Id, OrderStatus.Preparing);
        await database.Service.UpdateStatusAsync(created.Id, OrderStatus.Shipped);

        await Assert.ThrowsAsync<DomainException>(() => database.Service.CancelOrderAsync(created.Id));

        var storedOrder = await database.DbContext.Orders.SingleAsync(order => order.Id == created.Id);
        Assert.Equal(OrderStatus.Shipped, storedOrder.Status);
        Assert.Equal(3, await database.StockQuantityAsync(product.Id));
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsInvalidStatusTransition()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 32m, stockQuantity: 5);
        var created = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 1)]));

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            database.Service.UpdateStatusAsync(created.Id, OrderStatus.Delivered));

        Assert.Contains("cannot move", exception.Message, StringComparison.OrdinalIgnoreCase);

        var storedOrder = await database.DbContext.Orders.SingleAsync(order => order.Id == created.Id);
        Assert.Equal(OrderStatus.Pending, storedOrder.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WritesStatusHistory()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 32m, stockQuantity: 5);
        var created = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 1)]));

        await database.Service.UpdateStatusAsync(created.Id, OrderStatus.Confirmed);

        var history = await database.DbContext.OrderStatusHistories
            .SingleAsync(history => history.FromStatus == OrderStatus.Pending
                                    && history.ToStatus == OrderStatus.Confirmed);
        Assert.Equal(created.Id, history.OrderId);
    }

    [Fact]
    public async Task RecordPaymentAsync_StoresPaymentLedgerForOrder()
    {
        await using var database = new TestDatabase();
        var (customer, product) = await database.SeedCustomerAndProductAsync(price: 32m, stockQuantity: 5);
        var created = await database.Service.CreateOrderAsync(new CreateOrderRequest(
            customer.Id,
            [new CreateOrderItemRequest(product.Id, 2)]));

        var payment = await database.Service.RecordPaymentAsync(
            created.Id,
            new CreatePaymentRequest(PaymentMethod.Card, PaymentStatus.Paid, 64m));

        Assert.Equal(created.Id, payment.OrderId);
        Assert.Equal(PaymentMethod.Card, payment.PaymentMethod);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(64m, payment.Amount);
        Assert.NotNull(payment.PaidAt);
    }

    [Fact]
    public async Task OrderStatus_IsStoredAsIntegerColumn()
    {
        await using var database = new TestDatabase();

        await using var command = database.Connection.CreateCommand();
        command.CommandText = "SELECT type FROM pragma_table_info('Orders') WHERE name = 'Status'";
        var statusColumnType = (string?)await command.ExecuteScalarAsync();

        Assert.Equal("INTEGER", statusColumnType);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public TestDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new AppDbContext(options);
            DbContext.Database.EnsureCreated();
            Service = new OrderService(DbContext);
        }

        public SqliteConnection Connection => _connection;

        public AppDbContext DbContext { get; }

        public OrderService Service { get; }

        public async Task<(Customer Customer, Product Product)> SeedCustomerAndProductAsync(
            decimal price,
            int stockQuantity)
        {
            var customer = new Customer
            {
                Name = "Retail Customer",
                Email = $"customer-{Guid.NewGuid():N}@example.com",
                Phone = "010-0000-0000"
            };
            var product = new Product
            {
                Sku = $"SKU-{Guid.NewGuid():N}",
                Name = "Cotton Shirt",
                Description = "Retail test product",
                Price = price,
                IsActive = true,
                Inventory = new Inventory
                {
                    Quantity = stockQuantity
                }
            };

            DbContext.Customers.Add(customer);
            DbContext.Products.Add(product);
            await DbContext.SaveChangesAsync();

            return (customer, product);
        }

        public Task<int> StockQuantityAsync(int productId)
        {
            return DbContext.Inventories
                .Where(inventory => inventory.ProductId == productId)
                .Select(inventory => inventory.Quantity)
                .SingleAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

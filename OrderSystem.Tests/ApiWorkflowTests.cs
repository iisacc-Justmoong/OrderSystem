using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderSystem.Data;
using Xunit;

namespace OrderSystem.Tests;

public sealed class ApiWorkflowTests
{
    [Fact]
    public async Task SwaggerEndpoint_IsAvailable()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/products", body);
        Assert.Contains("/api/inventory", body);
        Assert.Contains("/api/orders", body);
    }

    [Fact]
    public async Task RestApi_CreatesOrderAndExposesInventoryWorkflow()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();

        var product = await PostJsonAsync(client, "/api/products", new
        {
            sku = "BLACK-TSHIRT",
            name = "Black T-Shirt",
            description = "Cotton retail product",
            price = 30000m,
            isActive = true,
            initialStockQuantity = 3
        }, HttpStatusCode.Created);
        var productId = product.GetProperty("id").GetInt64();

        var updatedProduct = await PutJsonAsync(client, $"/api/products/{productId}", new
        {
            sku = "BLACK-TSHIRT",
            name = "Black T-Shirt Updated",
            description = "Updated cotton retail product",
            price = 31000m,
            isActive = true
        }, HttpStatusCode.OK);
        Assert.Equal("Black T-Shirt Updated", updatedProduct.GetProperty("name").GetString());

        var customer = await PostJsonAsync(client, "/api/customers", new
        {
            name = "Retail Customer",
            email = "customer@example.com",
            phone = "010-0000-0000"
        }, HttpStatusCode.Created);
        var customerId = customer.GetProperty("id").GetInt64();

        var order = await PostJsonAsync(client, "/api/orders", new
        {
            customerId,
            items = new[]
            {
                new
                {
                    productId,
                    quantity = 2
                }
            }
        }, HttpStatusCode.Created);

        Assert.True(order.GetProperty("orderId").GetInt64() > 0);
        Assert.StartsWith("ORD-", order.GetProperty("orderNumber").GetString());
        Assert.Equal("Pending", order.GetProperty("status").GetString());
        Assert.Equal(62000m, order.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(31000m, order.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal());

        var inventory = await GetJsonAsync(client, $"/api/inventory/{productId}", HttpStatusCode.OK);
        Assert.Equal(1, inventory.GetProperty("quantity").GetInt32());

        var adjustedInventory = await PostJsonAsync(client, $"/api/inventory/{productId}/adjust", new
        {
            quantityChange = 4,
            referenceType = "CycleCount"
        }, HttpStatusCode.OK);
        Assert.Equal(5, adjustedInventory.GetProperty("quantity").GetInt32());

        var inventoryList = await GetJsonAsync(client, "/api/inventory", HttpStatusCode.OK);
        Assert.Single(inventoryList.EnumerateArray());
    }

    [Fact]
    public async Task RestApi_NormalizesCustomerEmailAndRejectsDuplicateEmail()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();

        var customer = await PostJsonAsync(client, "/api/customers", new
        {
            name = "  Retail Customer  ",
            email = "  Retail.Customer@Example.COM  ",
            phone = "  010-2222-3333  "
        }, HttpStatusCode.Created);
        var customerId = customer.GetProperty("id").GetInt64();

        Assert.Equal("Retail Customer", customer.GetProperty("name").GetString());
        Assert.Equal("retail.customer@example.com", customer.GetProperty("email").GetString());
        Assert.Equal("010-2222-3333", customer.GetProperty("phone").GetString());

        var loadedCustomer = await GetJsonAsync(client, $"/api/customers/{customerId}", HttpStatusCode.OK);
        Assert.Equal("retail.customer@example.com", loadedCustomer.GetProperty("email").GetString());

        var duplicateResponse = await client.PostAsJsonAsync("/api/customers", new
        {
            name = "Duplicate Retail Customer",
            email = "RETAIL.CUSTOMER@example.com",
            phone = "010-4444-5555"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task RestApi_NormalizesProductSkuAndRejectsDuplicates()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();

        var firstProduct = await PostJsonAsync(client, "/api/products", new
        {
            sku = "  SKU-DUPLICATE  ",
            name = "  Duplicate Test Product  ",
            description = "  Duplicate product description  ",
            price = 100m,
            isActive = true,
            initialStockQuantity = 0
        }, HttpStatusCode.Created);

        Assert.Equal("SKU-DUPLICATE", firstProduct.GetProperty("sku").GetString());
        Assert.Equal("Duplicate Test Product", firstProduct.GetProperty("name").GetString());
        Assert.Equal("Duplicate product description", firstProduct.GetProperty("description").GetString());

        var duplicateCreateResponse = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-DUPLICATE",
            name = "Another Product",
            description = "Another duplicate product description",
            price = 100m,
            isActive = true,
            initialStockQuantity = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCreateResponse.StatusCode);

        var secondProduct = await PostJsonAsync(client, "/api/products", new
        {
            sku = "SKU-SECOND",
            name = "Second Product",
            description = "Second product description",
            price = 200m,
            isActive = true,
            initialStockQuantity = 0
        }, HttpStatusCode.Created);
        var secondProductId = secondProduct.GetProperty("id").GetInt64();

        var duplicateUpdateResponse = await client.PutAsJsonAsync($"/api/products/{secondProductId}", new
        {
            sku = "  SKU-DUPLICATE  ",
            name = "Second Product Renamed",
            description = "Second product description",
            price = 200m,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateUpdateResponse.StatusCode);

        var missingProductUpdateResponse = await client.PutAsJsonAsync("/api/products/999999", new
        {
            sku = "SKU-DUPLICATE",
            name = "Missing Product",
            description = "Missing product description",
            price = 100m,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.NotFound, missingProductUpdateResponse.StatusCode);
    }

    [Fact]
    public async Task RestApi_ReturnsConflictWhenCurrentOrderStateBlocksAction()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();
        var (customerId, productId) = await SeedOrderPrerequisitesAsync(client, stockQuantity: 2);

        var invalidTransition = await PostJsonAsync(client, "/api/orders", new
        {
            customerId,
            items = new[]
            {
                new
                {
                    productId,
                    quantity = 1
                }
            }
        }, HttpStatusCode.Created);
        var invalidTransitionOrderId = invalidTransition.GetProperty("orderId").GetInt64();

        var transitionResponse = await client.PatchAsJsonAsync(
            $"/api/orders/{invalidTransitionOrderId}/status",
            new { status = "Delivered" });
        Assert.Equal(HttpStatusCode.Conflict, transitionResponse.StatusCode);

        var shippableOrder = await PostJsonAsync(client, "/api/orders", new
        {
            customerId,
            items = new[]
            {
                new
                {
                    productId,
                    quantity = 1
                }
            }
        }, HttpStatusCode.Created);
        var shippableOrderId = shippableOrder.GetProperty("orderId").GetInt64();
        await PatchStatusAsync(client, shippableOrderId, "Confirmed");
        await PatchStatusAsync(client, shippableOrderId, "Preparing");
        await PatchStatusAsync(client, shippableOrderId, "Shipped");

        var cancelResponse = await client.PostAsync($"/api/orders/{shippableOrderId}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task RestApi_ReturnsBadRequestWhenOrderExceedsStock()
    {
        await using var factory = new TestApplicationFactory();
        var client = factory.CreateClient();
        var (customerId, productId) = await SeedOrderPrerequisitesAsync(client, stockQuantity: 1);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            items = new[]
            {
                new
                {
                    productId,
                    quantity = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<(long CustomerId, long ProductId)> SeedOrderPrerequisitesAsync(
        HttpClient client,
        int stockQuantity)
    {
        var product = await PostJsonAsync(client, "/api/products", new
        {
            sku = $"SKU-{Guid.NewGuid():N}",
            name = "Seed Product",
            description = "Seed product for API tests",
            price = 100m,
            isActive = true,
            initialStockQuantity = stockQuantity
        }, HttpStatusCode.Created);

        var customer = await PostJsonAsync(client, "/api/customers", new
        {
            name = "Seed Customer",
            email = $"seed-{Guid.NewGuid():N}@example.com",
            phone = "010-1111-2222"
        }, HttpStatusCode.Created);

        return (customer.GetProperty("id").GetInt64(), product.GetProperty("id").GetInt64());
    }

    private static async Task PatchStatusAsync(HttpClient client, long orderId, string status)
    {
        var response = await client.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<JsonElement> GetJsonAsync(
        HttpClient client,
        string uri,
        HttpStatusCode expectedStatusCode)
    {
        var response = await client.GetAsync(uri);
        Assert.Equal(expectedStatusCode, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> PostJsonAsync(
        HttpClient client,
        string uri,
        object request,
        HttpStatusCode expectedStatusCode)
    {
        var response = await client.PostAsJsonAsync(uri, request);
        Assert.Equal(expectedStatusCode, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> PutJsonAsync(
        HttpClient client,
        string uri,
        object request,
        HttpStatusCode expectedStatusCode)
    {
        var response = await client.PutAsJsonAsync(uri, request);
        Assert.Equal(expectedStatusCode, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private sealed class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var dbContextOptions = services.SingleOrDefault(
                    descriptor => descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextOptions is not null)
                {
                    services.Remove(dbContextOptions);
                }

                services.AddSingleton<DbConnection>(_ =>
                {
                    var connection = new SqliteConnection("Data Source=:memory:");
                    connection.Open();
                    return connection;
                });
                services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                {
                    options.UseSqlite(serviceProvider.GetRequiredService<DbConnection>());
                });
            });
        }
    }
}

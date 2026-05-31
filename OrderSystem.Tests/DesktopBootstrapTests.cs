using OrderSystem.Desktop.ViewModels;
using Xunit;

namespace OrderSystem.Tests;

public sealed class DesktopBootstrapTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void MainWindowViewModel_ProvidesShoppingOperationsAndOutputConsole()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");

        Assert.Equal("Retail Order System", viewModel.Title);
        Assert.Equal("Operations Console", viewModel.WorkspaceTitle);
        Assert.Equal("http://localhost:5184", viewModel.ApiBaseUrl);
        Assert.Equal("Operation Console", viewModel.OperationConsoleTitle);
        Assert.Equal("Output Console", viewModel.OutputConsoleTitle);
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Browse Catalog");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Add T-Shirt to Cart");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Checkout Cart");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Confirm Latest Order");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Record Card Payment");
        Assert.Contains(viewModel.Operations, operation => operation.Method == "GET");
        Assert.Contains(viewModel.Operations, operation => operation.Method == "POST");
        Assert.Contains(viewModel.Operations, operation => operation.Method == "PATCH");
        Assert.NotNull(viewModel.SelectedOperation);
        Assert.Equal("GET", viewModel.RequestMethod);
        Assert.Equal("/api/products", viewModel.RequestPath);
        Assert.Contains("Mini Storefront", viewModel.OutputText);
        Assert.Contains("Catalog", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_SelectionChangesOutputConsoleScreen()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");
        var ordersOperation = viewModel.Operations.Single(operation => operation.Name == "View Orders");

        viewModel.SelectedOperation = ordersOperation;

        Assert.Contains("Orders", viewModel.OutputText);
        Assert.Contains("No submitted orders", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_ExecutesCartAndCheckoutAsMiniStorefront()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");
        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Add T-Shirt to Cart");
        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("Black T-Shirt M x 1", viewModel.OutputText);
        Assert.Contains("Cart total: 29,000", viewModel.OutputText);

        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Checkout Cart");
        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("Order #1001", viewModel.OutputText);
        Assert.Contains("Pending", viewModel.OutputText);
        Assert.Contains("Cart: empty", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_UsesEditableRequestValuesWhenRunningCartAction()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");
        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Add T-Shirt to Cart");
        viewModel.ApiBaseUrl = "http://localhost:7001";
        viewModel.RequestMethod = "POST";
        viewModel.RequestPath = "/custom/cart/items";
        viewModel.RequestBody = """
                                {
                                  "sku": "NOTE-A5-GRID",
                                  "quantity": 3
                                }
                                """;

        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("Request URL: http://localhost:7001/custom/cart/items", viewModel.OutputText);
        Assert.Contains("A5 Grid Notebook x 3", viewModel.OutputText);
        Assert.Contains("Cart total: 27,000", viewModel.OutputText);
        Assert.DoesNotContain("Black T-Shirt M x 1", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_UsesEditableStatusAndPaymentPayloads()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");
        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Add T-Shirt to Cart");
        viewModel.ExecuteSelectedOperationCommand.Execute(null);
        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Checkout Cart");
        viewModel.RequestBody = """
                                {
                                  "customerId": 77,
                                  "items": "current cart"
                                }
                                """;
        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Confirm Latest Order");
        viewModel.RequestBody = """
                                {
                                  "status": "Preparing"
                                }
                                """;
        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("Customer #77", viewModel.OutputText);
        Assert.Contains("Preparing", viewModel.OutputText);

        viewModel.SelectedOperation = viewModel.Operations.Single(operation => operation.Name == "Record Card Payment");
        viewModel.RequestBody = """
                                {
                                  "paymentMethod": "BankTransfer",
                                  "status": "Authorized"
                                }
                                """;
        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("BankTransfer Authorized", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_CanClearOutputConsole()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");

        viewModel.ExecuteSelectedOperationCommand.Execute(null);
        viewModel.ClearOutputCommand.Execute(null);

        Assert.Contains("Output cleared", viewModel.OutputText);
    }

    [Fact]
    public void Solution_RegistersAvaloniaDesktopProject()
    {
        var solution = File.ReadAllText(Path.Combine(RepositoryRoot, "OrderSystem.sln"));

        Assert.Contains("OrderSystem.Desktop", solution);
        Assert.Contains(@"OrderSystem.Desktop\OrderSystem.Desktop.csproj", solution);
    }

    [Fact]
    public void Solution_UsesDesktopProjectAsDefaultStartupCandidate()
    {
        var solution = File.ReadAllText(Path.Combine(RepositoryRoot, "OrderSystem.sln"));
        var firstProjectLine = solution
            .Split(Environment.NewLine)
            .First(line => line.StartsWith("Project(", StringComparison.Ordinal));

        Assert.Contains("OrderSystem.Desktop", firstProjectLine);
        Assert.Contains(@"OrderSystem.Desktop\OrderSystem.Desktop.csproj", firstProjectLine);
    }
}

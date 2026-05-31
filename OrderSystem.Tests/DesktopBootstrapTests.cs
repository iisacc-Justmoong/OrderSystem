using OrderSystem.Desktop.ViewModels;
using Xunit;

namespace OrderSystem.Tests;

public sealed class DesktopBootstrapTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void MainWindowViewModel_ProvidesMiniShoppingMallCatalogAndCart()
    {
        var viewModel = MainWindowViewModel.CreateDefault();

        Assert.Equal("Retail Order System", viewModel.Title);
        Assert.Equal("Mini Shopping Mall", viewModel.WorkspaceTitle);
        Assert.Equal("Product Catalog", viewModel.CatalogTitle);
        Assert.Equal("Shopping Cart", viewModel.CartTitle);
        Assert.Equal("Checkout", viewModel.CheckoutTitle);
        Assert.Equal("Shopping Console", viewModel.ConsoleTitle);
        Assert.True(viewModel.Products.Count >= 6);
        Assert.Contains(viewModel.Products, product => product.Sku == "TSHIRT-BLK-M");
        Assert.Contains(viewModel.Products, product => product.Sku == "MUG-STEEL-350");
        Assert.Contains(viewModel.Products, product => product.Sku == "NOTE-A5-GRID");
        Assert.NotNull(viewModel.SelectedProduct);
        Assert.Empty(viewModel.CartLines);
        Assert.Contains("Mini shopping mall opened", viewModel.ConsoleText);
    }

    [Fact]
    public void MainWindowViewModel_AddsSelectedProductsToCartWithChosenQuantity()
    {
        var viewModel = MainWindowViewModel.CreateDefault();
        viewModel.SelectedProduct = viewModel.Products.Single(product => product.Sku == "NOTE-A5-GRID");
        viewModel.SelectedQuantity = 3;

        viewModel.AddSelectedProductCommand.Execute(null);

        var line = Assert.Single(viewModel.CartLines);
        Assert.Equal("A5 Grid Notebook", line.ProductName);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(27000m, viewModel.CartTotal);
        Assert.Contains("Added A5 Grid Notebook x 3", viewModel.ConsoleText);
    }

    [Fact]
    public void MainWindowViewModel_UpdatesCartLineQuantityAndRemovesLine()
    {
        var viewModel = MainWindowViewModel.CreateDefault();
        viewModel.AddSelectedProductCommand.Execute(null);
        viewModel.SelectedCartLine = viewModel.CartLines.Single();

        viewModel.IncreaseSelectedCartLineCommand.Execute(null);
        Assert.Equal(2, viewModel.SelectedCartLine.Quantity);

        viewModel.DecreaseSelectedCartLineCommand.Execute(null);
        Assert.Equal(1, viewModel.SelectedCartLine.Quantity);

        viewModel.RemoveSelectedCartLineCommand.Execute(null);
        Assert.Empty(viewModel.CartLines);
        Assert.Contains("Removed Black T-Shirt M from cart", viewModel.ConsoleText);
    }

    [Fact]
    public void MainWindowViewModel_CheckoutCreatesOrderFromCartAndCustomerInputs()
    {
        var viewModel = MainWindowViewModel.CreateDefault();
        viewModel.SelectedProduct = viewModel.Products.Single(product => product.Sku == "MUG-STEEL-350");
        viewModel.SelectedQuantity = 2;
        viewModel.AddSelectedProductCommand.Execute(null);
        viewModel.CustomerName = "Kim Minsoo";
        viewModel.ShippingAddress = "Seoul Mapo";
        viewModel.PaymentMethod = "Card";

        viewModel.PlaceOrderCommand.Execute(null);

        Assert.Empty(viewModel.CartLines);
        Assert.Equal("Order #1001", viewModel.LatestOrderNumber);
        Assert.Equal("Paid", viewModel.LatestOrderStatus);
        Assert.Equal(36000m, viewModel.LatestOrderTotal);
        Assert.Contains("Order #1001 placed by Kim Minsoo", viewModel.ConsoleText);
        Assert.Contains("Payment accepted by Card", viewModel.ConsoleText);
    }

    [Fact]
    public void MainWindowViewModel_RejectsCheckoutWithoutCustomerAndCart()
    {
        var viewModel = MainWindowViewModel.CreateDefault();

        viewModel.PlaceOrderCommand.Execute(null);
        Assert.Contains("Cannot place order because the cart is empty", viewModel.ConsoleText);

        viewModel.AddSelectedProductCommand.Execute(null);
        viewModel.CustomerName = "";
        viewModel.PlaceOrderCommand.Execute(null);

        Assert.Contains("Customer name is required", viewModel.ConsoleText);
    }

    [Fact]
    public void MainWindowViewModel_CanClearShoppingConsole()
    {
        var viewModel = MainWindowViewModel.CreateDefault();

        viewModel.AddSelectedProductCommand.Execute(null);
        viewModel.ClearConsoleCommand.Execute(null);

        Assert.Equal("Shopping console cleared.", viewModel.ConsoleText);
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

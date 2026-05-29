using OrderSystem.Desktop.ViewModels;
using Xunit;

namespace OrderSystem.Tests;

public sealed class DesktopBootstrapTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void MainWindowViewModel_ProvidesSingleOperationAndOutputConsole()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");

        Assert.Equal("Retail Order System", viewModel.Title);
        Assert.Equal("Operations Console", viewModel.WorkspaceTitle);
        Assert.Equal("http://localhost:5184", viewModel.ApiBaseUrl);
        Assert.Equal("Operation Console", viewModel.OperationConsoleTitle);
        Assert.Equal("Output Console", viewModel.OutputConsoleTitle);
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Create Product");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Create Order");
        Assert.Contains(viewModel.Operations, operation => operation.Name == "Adjust Inventory");
        Assert.NotNull(viewModel.SelectedOperation);
        Assert.Contains("\"sku\"", viewModel.RequestBody);
        Assert.Contains("Ready", viewModel.OutputText);
    }

    [Fact]
    public void MainWindowViewModel_RecordsSelectedOperationInOutputConsole()
    {
        var viewModel = MainWindowViewModel.CreateDefault("http://localhost:5184");

        viewModel.ExecuteSelectedOperationCommand.Execute(null);

        Assert.Contains("POST /api/products", viewModel.OutputText);
        Assert.Contains("Request Body", viewModel.OutputText);
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

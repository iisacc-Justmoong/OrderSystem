using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace OrderSystem.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private ConsoleOperationViewModel? _selectedOperation;
    private string _requestBody;
    private string _outputText;

    private MainWindowViewModel(
        string title,
        string workspaceTitle,
        string apiBaseUrl,
        IReadOnlyList<ConsoleOperationViewModel> operations)
    {
        Title = title;
        WorkspaceTitle = workspaceTitle;
        ApiBaseUrl = apiBaseUrl;
        OperationConsoleTitle = "Operation Console";
        OutputConsoleTitle = "Output Console";
        Operations = operations;
        _selectedOperation = operations.FirstOrDefault();
        _requestBody = _selectedOperation?.SampleBody ?? string.Empty;
        _outputText = CreateReadyOutput(apiBaseUrl);
        ExecuteSelectedOperationCommand = new RelayCommand(ExecuteSelectedOperation);
        ClearOutputCommand = new RelayCommand(ClearOutput);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public string WorkspaceTitle { get; }

    public string ApiBaseUrl { get; }

    public string OperationConsoleTitle { get; }

    public string OutputConsoleTitle { get; }

    public IReadOnlyList<ConsoleOperationViewModel> Operations { get; }

    public ConsoleOperationViewModel? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (_selectedOperation == value)
            {
                return;
            }

            _selectedOperation = value;
            RequestBody = value?.SampleBody ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string RequestBody
    {
        get => _requestBody;
        set
        {
            if (_requestBody == value)
            {
                return;
            }

            _requestBody = value;
            OnPropertyChanged();
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (_outputText == value)
            {
                return;
            }

            _outputText = value;
            OnPropertyChanged();
        }
    }

    public ICommand ExecuteSelectedOperationCommand { get; }

    public ICommand ClearOutputCommand { get; }

    public static MainWindowViewModel CreateDefault(string apiBaseUrl = "http://localhost:5000")
    {
        return new MainWindowViewModel(
            "Retail Order System",
            "Operations Console",
            apiBaseUrl,
            [
                new(
                    "Create Product",
                    "POST",
                    "/api/products",
                    """
                    {
                      "sku": "TSHIRT-BLK-M",
                      "name": "Black T-Shirt M",
                      "description": "Black cotton t-shirt",
                      "price": 29000,
                      "isActive": true,
                      "initialStockQuantity": 50
                    }
                    """),
                new(
                    "Create Customer",
                    "POST",
                    "/api/customers",
                    """
                    {
                      "name": "Kim Minsoo",
                      "email": "minsoo@example.com",
                      "phone": "010-1111-2222"
                    }
                    """),
                new(
                    "Create Order",
                    "POST",
                    "/api/orders",
                    """
                    {
                      "customerId": 1,
                      "items": [
                        {
                          "productId": 1,
                          "quantity": 2
                        }
                      ]
                    }
                    """),
                new(
                    "Adjust Inventory",
                    "POST",
                    "/api/inventory/1/adjust",
                    """
                    {
                      "quantityChange": 10,
                      "referenceType": "ManualAdjustment"
                    }
                    """),
                new(
                    "Cancel Order",
                    "POST",
                    "/api/orders/1/cancel",
                    "{}")
            ]);
    }

    private void ExecuteSelectedOperation()
    {
        if (SelectedOperation is null)
        {
            AppendOutput("No operation selected.");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"[{DateTimeOffset.Now:HH:mm:ss}] {SelectedOperation.Method} {SelectedOperation.Path}");
        builder.AppendLine($"Base URL: {ApiBaseUrl}");
        builder.AppendLine("Request Body:");
        builder.AppendLine(string.IsNullOrWhiteSpace(RequestBody) ? "{}" : RequestBody);
        builder.AppendLine("Status: staged for REST API execution");
        AppendOutput(builder.ToString());
    }

    private void ClearOutput()
    {
        OutputText = $"[{DateTimeOffset.Now:HH:mm:ss}] Output cleared.";
    }

    private void AppendOutput(string text)
    {
        OutputText = $"{OutputText.TrimEnd()}{Environment.NewLine}{Environment.NewLine}{text.TrimEnd()}";
    }

    private static string CreateReadyOutput(string apiBaseUrl)
    {
        return $"[{DateTimeOffset.Now:HH:mm:ss}] Ready. API base URL: {apiBaseUrl}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record ConsoleOperationViewModel(
    string Name,
    string Method,
    string Path,
    string SampleBody);

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace OrderSystem.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly List<StoreProduct> _catalog = CreateCatalog();
    private readonly List<CartLine> _cart = [];
    private readonly List<StoreOrder> _orders = [];
    private readonly List<string> _activity = [];
    private int _nextOrderNumber = 1001;
    private ConsoleOperationViewModel? _selectedOperation;
    private string _apiBaseUrl;
    private string _requestMethod;
    private string _requestPath;
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
        OperationConsoleTitle = "Operation Console";
        OutputConsoleTitle = "Output Console";
        Operations = operations;
        _selectedOperation = operations.FirstOrDefault();
        _apiBaseUrl = apiBaseUrl;
        _requestMethod = _selectedOperation?.Method ?? "GET";
        _requestPath = _selectedOperation?.Path ?? "/";
        _requestBody = _selectedOperation?.SampleBody ?? string.Empty;
        AddActivity("Storefront ready.");
        _outputText = CreateStorefrontOutput(_selectedOperation?.Screen ?? "Catalog");
        ExecuteSelectedOperationCommand = new RelayCommand(ExecuteSelectedOperation);
        ClearOutputCommand = new RelayCommand(ClearOutput);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public string WorkspaceTitle { get; }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (_apiBaseUrl == value)
            {
                return;
            }

            _apiBaseUrl = value;
            OnPropertyChanged();
            OutputText = CreateStorefrontOutput(SelectedOperation?.Screen ?? "Catalog");
        }
    }

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
            RequestMethod = value?.Method ?? string.Empty;
            RequestPath = value?.Path ?? string.Empty;
            RequestBody = value?.SampleBody ?? string.Empty;
            OutputText = CreateStorefrontOutput(value?.Screen ?? "Catalog");
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

    public string RequestMethod
    {
        get => _requestMethod;
        set
        {
            if (_requestMethod == value)
            {
                return;
            }

            _requestMethod = value;
            OnPropertyChanged();
            OutputText = CreateStorefrontOutput(SelectedOperation?.Screen ?? "Catalog");
        }
    }

    public string RequestPath
    {
        get => _requestPath;
        set
        {
            if (_requestPath == value)
            {
                return;
            }

            _requestPath = value;
            OnPropertyChanged();
            OutputText = CreateStorefrontOutput(SelectedOperation?.Screen ?? "Catalog");
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

    public string CartSummary
    {
        get
        {
            var quantity = CartQuantity;
            return quantity == 0
                ? "Cart: empty"
                : $"Cart: {quantity} item{Pluralize(quantity)} / {ToMoney(CartTotal)}";
        }
    }

    public string LatestOrderSummary
    {
        get
        {
            var latestOrder = _orders.LastOrDefault();
            return latestOrder is null
                ? "Orders: none"
                : $"Latest: Order #{latestOrder.Number} / Customer #{latestOrder.CustomerId} / {latestOrder.Status} / {latestOrder.PaymentSummary}";
        }
    }

    public string InventorySummary => $"Inventory: {_catalog.Sum(product => product.Stock)} units";

    public static MainWindowViewModel CreateDefault(string apiBaseUrl = "http://localhost:5000")
    {
        return new MainWindowViewModel(
            "Retail Order System",
            "Operations Console",
            apiBaseUrl,
            [
                new(
                    "Browse Catalog",
                    "GET",
                    "/api/products",
                    "{}",
                    "Catalog",
                    ConsoleOperationKind.BrowseCatalog),
                new(
                    "Add T-Shirt to Cart",
                    "POST",
                    "/shop/cart/items",
                    """
                    {
                      "sku": "TSHIRT-BLK-M",
                      "quantity": 1
                    }
                    """,
                    "Cart",
                    ConsoleOperationKind.AddTShirtToCart),
                new(
                    "Add Travel Mug to Cart",
                    "POST",
                    "/shop/cart/items",
                    """
                    {
                      "sku": "MUG-STEEL-350",
                      "quantity": 1
                    }
                    """,
                    "Cart",
                    ConsoleOperationKind.AddMugToCart),
                new(
                    "View Cart",
                    "GET",
                    "/shop/cart",
                    "{}",
                    "Cart",
                    ConsoleOperationKind.ViewCart),
                new(
                    "Checkout Cart",
                    "POST",
                    "/api/orders",
                    """
                    {
                      "customerId": 1,
                      "items": "current cart"
                    }
                    """,
                    "Orders",
                    ConsoleOperationKind.CheckoutCart),
                new(
                    "View Orders",
                    "GET",
                    "/api/orders",
                    "{}",
                    "Orders",
                    ConsoleOperationKind.ViewOrders),
                new(
                    "Confirm Latest Order",
                    "PATCH",
                    "/api/orders/{latest}/status",
                    """
                    {
                      "status": "Confirmed"
                    }
                    """,
                    "Orders",
                    ConsoleOperationKind.ConfirmLatestOrder),
                new(
                    "Record Card Payment",
                    "POST",
                    "/api/orders/{latest}/payments",
                    """
                    {
                      "paymentMethod": "Card",
                      "status": "Paid"
                    }
                    """,
                    "Orders",
                    ConsoleOperationKind.RecordCardPayment),
                new(
                    "Cancel Latest Order",
                    "POST",
                    "/api/orders/{latest}/cancel",
                    "{}",
                    "Orders",
                    ConsoleOperationKind.CancelLatestOrder),
                new(
                    "Inventory Snapshot",
                    "GET",
                    "/api/inventory",
                    "{}",
                    "Inventory",
                    ConsoleOperationKind.InventorySnapshot)
            ]);
    }

    private void ExecuteSelectedOperation()
    {
        if (SelectedOperation is null)
        {
            AddActivity("No operation selected.");
            OutputText = CreateStorefrontOutput("Catalog");
            return;
        }

        var screen = SelectedOperation.Screen;
        switch (SelectedOperation.Kind)
        {
            case ConsoleOperationKind.BrowseCatalog:
                AddActivity("Catalog refreshed.");
                screen = "Catalog";
                break;
            case ConsoleOperationKind.AddTShirtToCart:
                AddCartItemFromRequestBody();
                screen = "Cart";
                break;
            case ConsoleOperationKind.AddMugToCart:
                AddCartItemFromRequestBody();
                screen = "Cart";
                break;
            case ConsoleOperationKind.ViewCart:
                AddActivity("Cart opened.");
                screen = "Cart";
                break;
            case ConsoleOperationKind.CheckoutCart:
                CheckoutCart();
                screen = "Orders";
                break;
            case ConsoleOperationKind.ViewOrders:
                AddActivity("Orders opened.");
                screen = "Orders";
                break;
            case ConsoleOperationKind.ConfirmLatestOrder:
                UpdateLatestOrderStatus();
                screen = "Orders";
                break;
            case ConsoleOperationKind.RecordCardPayment:
                RecordPaymentOnLatestOrder();
                screen = "Orders";
                break;
            case ConsoleOperationKind.CancelLatestOrder:
                CancelLatestOrder();
                screen = "Orders";
                break;
            case ConsoleOperationKind.InventorySnapshot:
                AddActivity("Inventory snapshot opened.");
                screen = "Inventory";
                break;
        }

        NotifyStorefrontStateChanged();
        OutputText = CreateStorefrontOutput(screen);
    }

    private void ClearOutput()
    {
        _activity.Clear();
        AddActivity("Output cleared.");
        OutputText = CreateStorefrontOutput(SelectedOperation?.Screen ?? "Catalog");
    }

    private void AddCartItemFromRequestBody()
    {
        if (!TryParseRequestBody(out var document) || document is null)
        {
            return;
        }

        using (document)
        {
            if (!TryReadString(document.RootElement, "sku", out var sku))
            {
                AddActivity("Cart request body must include sku.");
                return;
            }

            var quantity = TryReadInt(document.RootElement, "quantity", out var requestedQuantity)
                ? requestedQuantity
                : 1;
            AddProductToCart(sku, quantity);
        }
    }

    private void AddProductToCart(string sku, int quantity)
    {
        if (quantity <= 0)
        {
            AddActivity("Cart request quantity must be greater than 0.");
            return;
        }

        var product = _catalog.SingleOrDefault(item => string.Equals(item.Sku, sku, StringComparison.OrdinalIgnoreCase));
        if (product is null)
        {
            AddActivity($"Unknown SKU: {sku}.");
            return;
        }

        var existingQuantity = _cart
            .Where(line => line.Product == product)
            .Sum(line => line.Quantity);

        if (product.Stock < existingQuantity + quantity)
        {
            AddActivity($"{product.Name} stock is not enough.");
            return;
        }

        var cartLine = _cart.SingleOrDefault(line => line.Product == product);
        if (cartLine is null)
        {
            _cart.Add(new CartLine(product, quantity));
        }
        else
        {
            cartLine.Quantity += quantity;
        }

        AddActivity($"Added {product.Name} x {quantity} to cart.");
    }

    private void CheckoutCart()
    {
        if (_cart.Count == 0)
        {
            AddActivity("Checkout skipped because the cart is empty.");
            return;
        }

        var customerId = 1;
        if (TryParseRequestBody(out var document) && document is not null)
        {
            using (document)
            {
                if (TryReadInt(document.RootElement, "customerId", out var requestedCustomerId))
                {
                    customerId = requestedCustomerId;
                }
            }
        }
        else
        {
            return;
        }

        var order = new StoreOrder(
            _nextOrderNumber++,
            customerId,
            "Pending",
            "Unpaid",
            "None",
            _cart.Select(line => new StoreOrderLine(
                line.Product.Name,
                line.Product.Sku,
                line.Quantity,
                line.Product.Price)).ToList());

        foreach (var line in _cart)
        {
            line.Product.Stock -= line.Quantity;
        }

        _orders.Add(order);
        _cart.Clear();
        AddActivity($"Created Order #{order.Number} for Customer #{customerId} from cart.");
    }

    private void UpdateLatestOrderStatus()
    {
        var latestOrder = _orders.LastOrDefault();
        if (latestOrder is null)
        {
            AddActivity("No order is available for status update.");
            return;
        }

        if (!TryParseRequestBody(out var document) || document is null)
        {
            return;
        }

        string status;
        using (document)
        {
            if (!TryReadString(document.RootElement, "status", out var requestedStatus))
            {
                AddActivity("Status request body must include status.");
                return;
            }

            status = requestedStatus;
            latestOrder.Status = status;
        }

        AddActivity($"Order #{latestOrder.Number} moved to {status}.");
    }

    private void RecordPaymentOnLatestOrder()
    {
        var latestOrder = _orders.LastOrDefault();
        if (latestOrder is null)
        {
            AddActivity("No order is available for payment.");
            return;
        }

        if (!TryParseRequestBody(out var document) || document is null)
        {
            return;
        }

        using (document)
        {
            if (!TryReadString(document.RootElement, "paymentMethod", out var paymentMethod))
            {
                AddActivity("Payment request body must include paymentMethod.");
                return;
            }

            if (!TryReadString(document.RootElement, "status", out var status))
            {
                AddActivity("Payment request body must include status.");
                return;
            }

            latestOrder.PaymentMethod = paymentMethod;
            latestOrder.PaymentStatus = status;
            AddActivity($"Recorded {paymentMethod} payment for Order #{latestOrder.Number}.");
        }
    }

    private void CancelLatestOrder()
    {
        var latestOrder = _orders.LastOrDefault();
        if (latestOrder is null)
        {
            AddActivity("No order is available for cancellation.");
            return;
        }

        if (latestOrder.Status == "Cancelled")
        {
            AddActivity($"Order #{latestOrder.Number} is already cancelled.");
            return;
        }

        latestOrder.Status = "Cancelled";
        foreach (var line in latestOrder.Lines)
        {
            var product = _catalog.Single(item => item.Sku == line.Sku);
            product.Stock += line.Quantity;
        }

        AddActivity($"Cancelled Order #{latestOrder.Number} and restored inventory.");
    }

    private string CreateStorefrontOutput(string screen)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Mini Storefront");
        builder.AppendLine($"API base URL: {ApiBaseUrl}");
        if (SelectedOperation is not null)
        {
            builder.AppendLine($"Selected: {SelectedOperation.Name}");
        }

        builder.AppendLine($"Request: {RequestMethod} {RequestPath}");
        builder.AppendLine($"Request URL: {BuildRequestUrl()}");
        builder.AppendLine();
        AppendStoreSummary(builder);
        builder.AppendLine();

        switch (screen)
        {
            case "Cart":
                AppendCart(builder);
                break;
            case "Orders":
                AppendOrders(builder);
                break;
            case "Inventory":
                AppendInventory(builder);
                break;
            default:
                AppendCatalog(builder);
                break;
        }

        builder.AppendLine();
        AppendActivity(builder);
        return builder.ToString().TrimEnd();
    }

    private void AppendStoreSummary(StringBuilder builder)
    {
        builder.AppendLine(CartSummary);
        builder.AppendLine(LatestOrderSummary);
        builder.AppendLine(InventorySummary);
    }

    private void AppendCatalog(StringBuilder builder)
    {
        builder.AppendLine("Catalog");
        foreach (var product in _catalog)
        {
            builder.AppendLine($"- {product.Name} | {product.Sku} | {ToMoney(product.Price)} | stock {product.Stock}");
        }
    }

    private void AppendCart(StringBuilder builder)
    {
        builder.AppendLine("Cart");
        if (_cart.Count == 0)
        {
            builder.AppendLine("Cart: empty");
            return;
        }

        foreach (var line in _cart)
        {
            builder.AppendLine($"- {line.Product.Name} x {line.Quantity} | {ToMoney(line.LineTotal)}");
        }

        builder.AppendLine($"Cart total: {ToMoney(CartTotal)}");
    }

    private void AppendOrders(StringBuilder builder)
    {
        builder.AppendLine("Orders");
        if (_orders.Count == 0)
        {
            builder.AppendLine("No submitted orders.");
            return;
        }

        foreach (var order in _orders.OrderByDescending(order => order.Number))
        {
            builder.AppendLine($"- Order #{order.Number} | Customer #{order.CustomerId} | {order.Status} | {order.PaymentSummary} | {ToMoney(order.Total)}");
            foreach (var line in order.Lines)
            {
                builder.AppendLine($"  {line.ProductName} x {line.Quantity} | {ToMoney(line.LineTotal)}");
            }
        }
    }

    private void AppendInventory(StringBuilder builder)
    {
        builder.AppendLine("Inventory");
        foreach (var product in _catalog)
        {
            builder.AppendLine($"- {product.Name} | stock {product.Stock}");
        }
    }

    private void AppendActivity(StringBuilder builder)
    {
        builder.AppendLine("Activity");
        foreach (var item in _activity.Take(5))
        {
            builder.AppendLine($"- {item}");
        }
    }

    private void AddActivity(string message)
    {
        _activity.Insert(0, $"[{DateTimeOffset.Now:HH:mm:ss}] {message}");
        if (_activity.Count > 12)
        {
            _activity.RemoveRange(12, _activity.Count - 12);
        }
    }

    private void NotifyStorefrontStateChanged()
    {
        OnPropertyChanged(nameof(CartSummary));
        OnPropertyChanged(nameof(LatestOrderSummary));
        OnPropertyChanged(nameof(InventorySummary));
    }

    private string BuildRequestUrl()
    {
        var baseUrl = ApiBaseUrl.Trim().TrimEnd('/');
        var path = RequestPath.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return baseUrl;
        }

        return path.StartsWith("/", StringComparison.Ordinal)
            ? $"{baseUrl}{path}"
            : $"{baseUrl}/{path}";
    }

    private bool TryParseRequestBody(out JsonDocument? document)
    {
        document = null;
        var body = string.IsNullOrWhiteSpace(RequestBody) ? "{}" : RequestBody;
        try
        {
            document = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            AddActivity("Request body must be valid JSON.");
            return false;
        }
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out value);
    }

    private int CartQuantity => _cart.Sum(line => line.Quantity);

    private decimal CartTotal => _cart.Sum(line => line.LineTotal);

    private static string ToMoney(decimal amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string Pluralize(int quantity)
    {
        return quantity == 1 ? string.Empty : "s";
    }

    private static List<StoreProduct> CreateCatalog()
    {
        return
        [
            new("TSHIRT-BLK-M", "Black T-Shirt M", 29000m, 50),
            new("MUG-STEEL-350", "Insulated Travel Mug", 18000m, 30),
            new("NOTE-A5-GRID", "A5 Grid Notebook", 9000m, 80)
        ];
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record StoreProduct(string Sku, string Name, decimal Price, int InitialStock)
    {
        public int Stock { get; set; } = InitialStock;
    }

    private sealed class CartLine(StoreProduct product, int quantity)
    {
        public StoreProduct Product { get; } = product;

        public int Quantity { get; set; } = quantity;

        public decimal LineTotal => Product.Price * Quantity;
    }

    private sealed class StoreOrder(
        int number,
        int customerId,
        string status,
        string paymentStatus,
        string paymentMethod,
        IReadOnlyList<StoreOrderLine> lines)
    {
        public int Number { get; } = number;

        public int CustomerId { get; } = customerId;

        public string Status { get; set; } = status;

        public string PaymentStatus { get; set; } = paymentStatus;

        public string PaymentMethod { get; set; } = paymentMethod;

        public IReadOnlyList<StoreOrderLine> Lines { get; } = lines;

        public decimal Total => Lines.Sum(line => line.LineTotal);

        public string PaymentSummary => PaymentMethod == "None"
            ? PaymentStatus
            : $"{PaymentMethod} {PaymentStatus}";
    }

    private sealed record StoreOrderLine(string ProductName, string Sku, int Quantity, decimal UnitPrice)
    {
        public decimal LineTotal => UnitPrice * Quantity;
    }
}

public sealed record ConsoleOperationViewModel(
    string Name,
    string Method,
    string Path,
    string SampleBody,
    string Screen,
    ConsoleOperationKind Kind);

public enum ConsoleOperationKind
{
    BrowseCatalog,
    AddTShirtToCart,
    AddMugToCart,
    ViewCart,
    CheckoutCart,
    ViewOrders,
    ConfirmLatestOrder,
    RecordCardPayment,
    CancelLatestOrder,
    InventorySnapshot
}

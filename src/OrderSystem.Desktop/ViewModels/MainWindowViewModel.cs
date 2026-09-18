using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace OrderSystem.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly List<StoreOrderViewModel> _orders = [];
    private int _nextOrderNumber = 1001;
    private ProductViewModel? _selectedProduct;
    private CartLineViewModel? _selectedCartLine;
    private int _selectedQuantity = 1;
    private string _customerName = "Guest Customer";
    private string _shippingAddress = "Seoul Fulfillment Desk";
    private string _paymentMethod = "Card";
    private string _consoleText;

    private MainWindowViewModel()
    {
        Title = "Retail Order System";
        WorkspaceTitle = "Mini Shopping Mall";
        CatalogTitle = "Product Catalog";
        CartTitle = "Shopping Cart";
        CheckoutTitle = "Checkout";
        ConsoleTitle = "Shopping Console";
        Products = new ObservableCollection<ProductViewModel>(CreateProducts());
        CartLines = [];
        PaymentMethods = ["Card", "BankTransfer", "Cash", "Coupon"];
        _selectedProduct = Products.FirstOrDefault();
        _consoleText = CreateInitialConsoleText();

        AddSelectedProductCommand = new RelayCommand(AddSelectedProductToCart);
        IncreaseSelectedQuantityCommand = new RelayCommand(() => SelectedQuantity += 1);
        DecreaseSelectedQuantityCommand = new RelayCommand(() => SelectedQuantity -= 1);
        IncreaseSelectedCartLineCommand = new RelayCommand(IncreaseSelectedCartLine);
        DecreaseSelectedCartLineCommand = new RelayCommand(DecreaseSelectedCartLine);
        RemoveSelectedCartLineCommand = new RelayCommand(RemoveSelectedCartLine);
        ClearCartCommand = new RelayCommand(ClearCart);
        PlaceOrderCommand = new RelayCommand(PlaceOrder);
        ClearConsoleCommand = new RelayCommand(ClearConsole);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public string WorkspaceTitle { get; }

    public string CatalogTitle { get; }

    public string CartTitle { get; }

    public string CheckoutTitle { get; }

    public string ConsoleTitle { get; }

    public ObservableCollection<ProductViewModel> Products { get; }

    public ObservableCollection<CartLineViewModel> CartLines { get; }

    public IReadOnlyList<string> PaymentMethods { get; }

    public ProductViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct == value)
            {
                return;
            }

            _selectedProduct = value;
            OnPropertyChanged();
        }
    }

    public CartLineViewModel? SelectedCartLine
    {
        get => _selectedCartLine;
        set
        {
            if (_selectedCartLine == value)
            {
                return;
            }

            _selectedCartLine = value;
            OnPropertyChanged();
        }
    }

    public int SelectedQuantity
    {
        get => _selectedQuantity;
        set
        {
            var normalized = Math.Clamp(value, 1, 99);
            if (_selectedQuantity == normalized)
            {
                return;
            }

            _selectedQuantity = normalized;
            OnPropertyChanged();
        }
    }

    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (_customerName == value)
            {
                return;
            }

            _customerName = value;
            OnPropertyChanged();
        }
    }

    public string ShippingAddress
    {
        get => _shippingAddress;
        set
        {
            if (_shippingAddress == value)
            {
                return;
            }

            _shippingAddress = value;
            OnPropertyChanged();
        }
    }

    public string PaymentMethod
    {
        get => _paymentMethod;
        set
        {
            if (_paymentMethod == value)
            {
                return;
            }

            _paymentMethod = value;
            OnPropertyChanged();
        }
    }

    public string ConsoleText
    {
        get => _consoleText;
        private set
        {
            if (_consoleText == value)
            {
                return;
            }

            _consoleText = value;
            OnPropertyChanged();
        }
    }

    public string CartSummary => CartLines.Count == 0
        ? "Cart: empty"
        : $"Cart: {CartLines.Sum(line => line.Quantity)} item{Pluralize(CartLines.Sum(line => line.Quantity))} / {ToMoney(CartTotal)}";

    public decimal CartTotal => CartLines.Sum(line => line.LineTotal);

    public string LatestOrderNumber => _orders.LastOrDefault()?.OrderNumber ?? "No order";

    public string LatestOrderStatus => _orders.LastOrDefault()?.Status ?? "None";

    public decimal LatestOrderTotal => _orders.LastOrDefault()?.Total ?? 0m;

    public string LatestOrderSummary => _orders.LastOrDefault() is { } latestOrder
        ? $"{latestOrder.OrderNumber} / {latestOrder.CustomerName} / {latestOrder.Status} / {ToMoney(latestOrder.Total)}"
        : "No submitted order";

    public ICommand AddSelectedProductCommand { get; }

    public ICommand IncreaseSelectedQuantityCommand { get; }

    public ICommand DecreaseSelectedQuantityCommand { get; }

    public ICommand IncreaseSelectedCartLineCommand { get; }

    public ICommand DecreaseSelectedCartLineCommand { get; }

    public ICommand RemoveSelectedCartLineCommand { get; }

    public ICommand ClearCartCommand { get; }

    public ICommand PlaceOrderCommand { get; }

    public ICommand ClearConsoleCommand { get; }

    public static MainWindowViewModel CreateDefault(string apiBaseUrl = "http://localhost:5000")
    {
        _ = apiBaseUrl;
        return new MainWindowViewModel();
    }

    private void AddSelectedProductToCart()
    {
        if (SelectedProduct is null)
        {
            AppendConsole("No product selected.");
            return;
        }

        AddProductToCart(SelectedProduct, SelectedQuantity);
    }

    private void AddProductToCart(ProductViewModel product, int quantity)
    {
        var existingLine = CartLines.SingleOrDefault(line => line.Sku == product.Sku);
        var existingQuantity = existingLine?.Quantity ?? 0;
        if (existingQuantity + quantity > product.Stock)
        {
            AppendConsole($"{product.Name} stock is not enough. Requested {existingQuantity + quantity}, available {product.Stock}.");
            return;
        }

        if (existingLine is null)
        {
            var line = new CartLineViewModel(product, quantity);
            CartLines.Add(line);
            SelectedCartLine = line;
        }
        else
        {
            existingLine.Quantity += quantity;
            SelectedCartLine = existingLine;
        }

        AppendConsole($"Added {product.Name} x {quantity} to cart.");
        NotifyCartChanged();
    }

    private void IncreaseSelectedCartLine()
    {
        if (SelectedCartLine is null)
        {
            AppendConsole("Select a cart item before changing quantity.");
            return;
        }

        if (SelectedCartLine.Quantity >= SelectedCartLine.AvailableStock)
        {
            AppendConsole($"{SelectedCartLine.ProductName} stock limit reached.");
            return;
        }

        SelectedCartLine.Quantity += 1;
        AppendConsole($"Increased {SelectedCartLine.ProductName} to {SelectedCartLine.Quantity}.");
        NotifyCartChanged();
    }

    private void DecreaseSelectedCartLine()
    {
        if (SelectedCartLine is null)
        {
            AppendConsole("Select a cart item before changing quantity.");
            return;
        }

        if (SelectedCartLine.Quantity <= 1)
        {
            RemoveSelectedCartLine();
            return;
        }

        SelectedCartLine.Quantity -= 1;
        AppendConsole($"Decreased {SelectedCartLine.ProductName} to {SelectedCartLine.Quantity}.");
        NotifyCartChanged();
    }

    private void RemoveSelectedCartLine()
    {
        if (SelectedCartLine is null)
        {
            AppendConsole("Select a cart item before removing it.");
            return;
        }

        var removedName = SelectedCartLine.ProductName;
        CartLines.Remove(SelectedCartLine);
        SelectedCartLine = CartLines.FirstOrDefault();
        AppendConsole($"Removed {removedName} from cart.");
        NotifyCartChanged();
    }

    private void ClearCart()
    {
        CartLines.Clear();
        SelectedCartLine = null;
        AppendConsole("Cart cleared.");
        NotifyCartChanged();
    }

    private void PlaceOrder()
    {
        if (CartLines.Count == 0)
        {
            AppendConsole("Cannot place order because the cart is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            AppendConsole("Customer name is required before checkout.");
            return;
        }

        var orderNumber = $"Order #{_nextOrderNumber++}";
        var orderLines = CartLines
            .Select(line => new StoreOrderLineViewModel(line.ProductName, line.Sku, line.Quantity, line.UnitPrice))
            .ToList();
        var order = new StoreOrderViewModel(
            orderNumber,
            CustomerName.Trim(),
            string.IsNullOrWhiteSpace(ShippingAddress) ? "Pickup" : ShippingAddress.Trim(),
            PaymentMethod,
            "Paid",
            orderLines);

        foreach (var cartLine in CartLines)
        {
            var product = Products.Single(item => item.Sku == cartLine.Sku);
            product.Stock -= cartLine.Quantity;
        }

        _orders.Add(order);
        CartLines.Clear();
        SelectedCartLine = null;
        AppendConsole($"{order.OrderNumber} placed by {order.CustomerName}. Total {ToMoney(order.Total)}.");
        AppendConsole($"Payment accepted by {order.PaymentMethod}.");
        AppendConsole($"Shipping destination: {order.ShippingAddress}.");
        NotifyCartChanged();
        NotifyOrderChanged();
    }

    private void ClearConsole()
    {
        ConsoleText = "Shopping console cleared.";
    }

    private void AppendConsole(string message)
    {
        var builder = new StringBuilder(ConsoleText.TrimEnd());
        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append($"[{DateTimeOffset.Now:HH:mm:ss}] {message}");
        ConsoleText = builder.ToString();
    }

    private void NotifyCartChanged()
    {
        OnPropertyChanged(nameof(CartSummary));
        OnPropertyChanged(nameof(CartTotal));
    }

    private void NotifyOrderChanged()
    {
        OnPropertyChanged(nameof(LatestOrderNumber));
        OnPropertyChanged(nameof(LatestOrderStatus));
        OnPropertyChanged(nameof(LatestOrderTotal));
        OnPropertyChanged(nameof(LatestOrderSummary));
    }

    private static string CreateInitialConsoleText()
    {
        return $"[{DateTimeOffset.Now:HH:mm:ss}] Mini shopping mall opened.";
    }

    private static List<ProductViewModel> CreateProducts()
    {
        return
        [
            new("TSHIRT-BLK-M", "Black T-Shirt M", "Apparel", "Soft black cotton tee for daily wear.", 29000m, 50),
            new("MUG-STEEL-350", "Insulated Travel Mug", "Kitchen", "Steel tumbler that keeps drinks warm during delivery runs.", 18000m, 30),
            new("NOTE-A5-GRID", "A5 Grid Notebook", "Stationery", "Grid notebook for order notes and stock checks.", 9000m, 80),
            new("BAG-CANVAS-S", "Canvas Tote Bag", "Apparel", "Small canvas tote for light shopping.", 22000m, 25),
            new("LAMP-DESK-LED", "LED Desk Lamp", "Home", "Compact desk lamp with warm and cool light modes.", 45000m, 14),
            new("SOAP-CITRUS-SET", "Citrus Soap Set", "Lifestyle", "Three-piece handmade citrus soap set.", 15000m, 40)
        ];
    }

    private static string ToMoney(decimal amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string Pluralize(int quantity)
    {
        return quantity == 1 ? string.Empty : "s";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ProductViewModel(
    string sku,
    string name,
    string category,
    string description,
    decimal price,
    int stock) : INotifyPropertyChanged
{
    private int _stock = stock;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Sku { get; } = sku;

    public string Name { get; } = name;

    public string Category { get; } = category;

    public string Description { get; } = description;

    public decimal Price { get; } = price;

    public string PriceText => Price.ToString("N0", CultureInfo.InvariantCulture);

    public int Stock
    {
        get => _stock;
        set
        {
            if (_stock == value)
            {
                return;
            }

            _stock = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockText));
        }
    }

    public string StockText => $"Stock {Stock}";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class CartLineViewModel(ProductViewModel product, int quantity) : INotifyPropertyChanged
{
    private int _quantity = quantity;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Sku { get; } = product.Sku;

    public string ProductName { get; } = product.Name;

    public decimal UnitPrice { get; } = product.Price;

    public int AvailableStock => product.Stock;

    public int Quantity
    {
        get => _quantity;
        set
        {
            var normalized = Math.Clamp(value, 1, 99);
            if (_quantity == normalized)
            {
                return;
            }

            _quantity = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalText));
        }
    }

    public decimal LineTotal => UnitPrice * Quantity;

    public string UnitPriceText => UnitPrice.ToString("N0", CultureInfo.InvariantCulture);

    public string LineTotalText => LineTotal.ToString("N0", CultureInfo.InvariantCulture);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record StoreOrderViewModel(
    string OrderNumber,
    string CustomerName,
    string ShippingAddress,
    string PaymentMethod,
    string Status,
    IReadOnlyList<StoreOrderLineViewModel> Lines)
{
    public decimal Total => Lines.Sum(line => line.LineTotal);
}

public sealed record StoreOrderLineViewModel(string ProductName, string Sku, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

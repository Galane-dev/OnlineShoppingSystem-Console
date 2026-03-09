using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Repositories;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Shared fixture that wires up a complete, isolated service graph for every test.
/// Uses a temp-directory DataStore (no shared state between tests).
/// Call Reset() at the start of a test to get a completely blank slate.
/// </summary>
public sealed class TestFixture : IDisposable
{
    private string _tempDir;

    public DataStore        Store           { get; private set; } = null!;
    public AuthService      AuthService     { get; private set; } = null!;
    public ProductService   ProductService  { get; private set; } = null!;
    public CartService      CartService     { get; private set; } = null!;
    public PaymentService   PaymentService  { get; private set; } = null!;
    public OrderService     OrderService    { get; private set; } = null!;
    public ReviewService    ReviewService   { get; private set; } = null!;
    public WishlistService  WishlistService { get; private set; } = null!;

    public TestFixture()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"oss_test_{Guid.NewGuid():N}");
        Build();
    }

    /// <summary>Tears down the current store and rebuilds a completely fresh one.</summary>
    public void Reset()
    {
        Dispose();
        _tempDir = Path.Combine(Path.GetTempPath(), $"oss_test_{Guid.NewGuid():N}");
        Build();
    }

    private void Build()
    {
        Store = new DataStore(_tempDir);

        // Repositories
        var userRepository    = new UserRepository(Store);
        var productRepository = new ProductRepository(Store);
        var orderRepository   = new OrderRepository(Store);
        var paymentRepository = new PaymentRepository(Store);

        // Services — mirror Program.cs wiring exactly
        AuthService     = new AuthService(userRepository, Store);
        ProductService  = new ProductService(productRepository, Store);
        PaymentService  = new PaymentService(paymentRepository, Store);
        OrderService    = new OrderService(orderRepository, productRepository, PaymentService, Store);
        CartService     = new CartService(productRepository);
        ReviewService   = new ReviewService(productRepository, Store);
        WishlistService = new WishlistService(productRepository);
    }

    // ── Convenience factory methods ────────────────────────────────────────────

    /// <summary>Registers and returns a customer with a valid strong password.</summary>
    public Customer CreateCustomer(
        string  username      = "testuser",
        string  fullName      = "Test User",
        decimal walletBalance = 1000m)
    {
        var customer = AuthService.RegisterCustomer(
            username,
            $"{username}@test.com",
            "Password1!",
            fullName,
            "What was your first pet's name?",
            "fluffy");

        customer.WalletBalance = walletBalance;
        return customer;
    }

    /// <summary>Adds a product to the store and returns it.</summary>
    public Product CreateProduct(
        string  name     = "Test Product",
        decimal price    = 100m,
        int     stock    = 10,
        string  category = "General")
    {
        var product = new Product
        {
            Name          = name,
            Description   = $"{name} description",
            Category      = category,
            Price         = price,
            StockQuantity = stock
        };

        ProductService.Add(product);
        return product;
    }

    /// <summary>Places and returns a delivered order for the given customer and product.</summary>
    public Order CreateDeliveredOrder(Customer customer, Product product, int quantity = 1)
    {
        CartService.AddToCart(customer, product.Id, quantity);
        var order = OrderService.PlaceOrder(customer);

        if (!customer.OrderHistory.Contains(order))
            customer.OrderHistory.Add(order);

        OrderService.UpdateStatus(order.Id, OrderStatus.Delivered);
        return order;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}

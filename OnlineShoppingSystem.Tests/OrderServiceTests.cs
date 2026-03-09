using FluentAssertions;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for OrderService: placing orders, cancellations, returns,
/// stock side-effects, refunds, and status updates.
/// </summary>
public class OrderServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── PlaceOrder ─────────────────────────────────────────────────────────────

    [Fact]
    public void PlaceOrder_ValidCart_CreatesOrderWithCorrectTotal()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 200m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 2);

        var order = _fx.OrderService.PlaceOrder(customer);

        order.TotalAmount.Should().Be(400m);
        order.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public void PlaceOrder_ValidCart_DeductsWalletBalance()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 200m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        _fx.OrderService.PlaceOrder(customer);

        customer.WalletBalance.Should().Be(300m);
    }

    [Fact]
    public void PlaceOrder_ValidCart_DecrementsProductStock()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 10);
        _fx.CartService.AddToCart(customer, product.Id, 3);

        _fx.OrderService.PlaceOrder(customer);

        product.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void PlaceOrder_ValidCart_ClearsCartAfterCheckout()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        _fx.OrderService.PlaceOrder(customer);

        customer.Cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void PlaceOrder_EmptyCart_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);

        var act = () => _fx.OrderService.PlaceOrder(customer);

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty cart*");
    }

    [Fact]
    public void PlaceOrder_InsufficientWalletBalance_CancelsOrder()
    {
        var customer = _fx.CreateCustomer(walletBalance: 10m);
        var product  = _fx.CreateProduct("Laptop", price: 999m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        var act = () => _fx.OrderService.PlaceOrder(customer);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PlaceOrder_AddsOrderToStore()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        var order = _fx.OrderService.PlaceOrder(customer);

        _fx.OrderService.GetById(order.Id).Should().NotBeNull();
    }

    // ── CancelOrder ────────────────────────────────────────────────────────────

    [Fact]
    public void CancelOrder_PendingOrder_SetsStatusToCancelled()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);
        customer.OrderHistory.Add(order);

        _fx.OrderService.CancelOrder(customer, order.Id);

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void CancelOrder_PendingOrder_RefundsWallet()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 200m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);
        customer.OrderHistory.Add(order);

        _fx.OrderService.CancelOrder(customer, order.Id);

        customer.WalletBalance.Should().Be(500m); // full refund
    }

    [Fact]
    public void CancelOrder_DeliveredOrder_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        var order    = _fx.CreateDeliveredOrder(customer, product);

        var act = () => _fx.OrderService.CancelOrder(customer, order.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*delivered*");
    }

    [Fact]
    public void CancelOrder_AlreadyCancelled_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);
        customer.OrderHistory.Add(order);
        _fx.OrderService.CancelOrder(customer, order.Id);

        var act = () => _fx.OrderService.CancelOrder(customer, order.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already cancelled*");
    }

    [Fact]
    public void CancelOrder_OrderBelongingToOtherCustomer_Throws()
    {
        var alice   = _fx.CreateCustomer("alice", walletBalance: 500m);
        var bob     = _fx.CreateCustomer("bob",   walletBalance: 500m);
        var product = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(alice, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(alice);
        alice.OrderHistory.Add(order);

        // Bob should not be able to cancel Alice's order
        var act = () => _fx.OrderService.CancelOrder(bob, order.Id);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── ReturnOrder ────────────────────────────────────────────────────────────

    [Fact]
    public void ReturnOrder_DeliveredOrder_SetsStatusToCancelled()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        var order    = _fx.CreateDeliveredOrder(customer, product);

        _fx.OrderService.ReturnOrder(customer, order.Id);

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void ReturnOrder_DeliveredOrder_RefundsWallet()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 200m, stock: 5);
        var order    = _fx.CreateDeliveredOrder(customer, product);
        var balanceAfterPurchase = customer.WalletBalance;

        _fx.OrderService.ReturnOrder(customer, order.Id);

        customer.WalletBalance.Should().Be(balanceAfterPurchase + order.TotalAmount);
    }

    [Fact]
    public void ReturnOrder_DeliveredOrder_RestocksItems()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 10);
        var order    = _fx.CreateDeliveredOrder(customer, product, quantity: 3);
        var stockAfterPurchase = product.StockQuantity; // should be 7

        _fx.OrderService.ReturnOrder(customer, order.Id);

        product.StockQuantity.Should().Be(stockAfterPurchase + 3);
    }

    [Fact]
    public void ReturnOrder_NonDeliveredOrder_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);
        customer.OrderHistory.Add(order);

        var act = () => _fx.OrderService.ReturnOrder(customer, order.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*delivered*");
    }

    // ── UpdateStatus ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateStatus_ValidOrder_ChangesStatus()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);

        _fx.OrderService.UpdateStatus(order.Id, OrderStatus.Shipped);

        _fx.OrderService.GetById(order.Id)!.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void UpdateStatus_UnknownOrder_Throws()
    {
        var act = () => _fx.OrderService.UpdateStatus(99999, OrderStatus.Shipped);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── GetOrdersByCustomer ────────────────────────────────────────────────────

    [Fact]
    public void GetOrdersByCustomer_ReturnsOnlyThatCustomersOrders()
    {
        var alice   = _fx.CreateCustomer("alice", walletBalance: 500m);
        var bob     = _fx.CreateCustomer("bob",   walletBalance: 500m);
        var product = _fx.CreateProduct("Laptop", price: 100m, stock: 10);

        _fx.CartService.AddToCart(alice, product.Id, 1);
        _fx.OrderService.PlaceOrder(alice);

        var results = _fx.OrderService.GetOrdersByCustomer(alice.Id);

        results.Should().OnlyContain(o => o.CustomerId == alice.Id);
        results.Should().NotContain(o => o.CustomerId == bob.Id);
    }
}

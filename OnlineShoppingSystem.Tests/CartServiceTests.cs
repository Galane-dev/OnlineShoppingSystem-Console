using FluentAssertions;
using Xunit;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for CartService: adding items, removing items, quantity updates,
/// stock enforcement, and cart clearing.
/// </summary>
public class CartServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── AddToCart ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddToCart_ValidProduct_AppearsInCart()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 5);

        _fx.CartService.AddToCart(customer, product.Id, 1);

        customer.Cart.Items.Should().ContainSingle(i => i.ProductId == product.Id);
    }

    [Fact]
    public void AddToCart_IncreasesCartTotal()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", price: 500m, stock: 5);

        _fx.CartService.AddToCart(customer, product.Id, 2);

        customer.Cart.Total.Should().Be(1000m);
    }

    [Fact]
    public void AddToCart_SameProductTwice_AccumulatesQuantity()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 10);

        _fx.CartService.AddToCart(customer, product.Id, 2);
        _fx.CartService.AddToCart(customer, product.Id, 3);

        customer.Cart.Items.Single(i => i.ProductId == product.Id).Quantity.Should().Be(5);
    }

    [Fact]
    public void AddToCart_ExceedsStock_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 3);

        var act = () => _fx.CartService.AddToCart(customer, product.Id, 10);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddToCart_OutOfStockProduct_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 0);

        var act = () => _fx.CartService.AddToCart(customer, product.Id, 1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddToCart_ZeroQuantity_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 5);

        var act = () => _fx.CartService.AddToCart(customer, product.Id, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddToCart_UnknownProduct_Throws()
    {
        var customer = _fx.CreateCustomer();

        var act = () => _fx.CartService.AddToCart(customer, 99999, 1);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── RemoveFromCart ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveFromCart_ExistingItem_RemovesItFromCart()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        _fx.CartService.RemoveFromCart(customer, product.Id);

        customer.Cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFromCart_NonExistentItem_Throws()
    {
        var customer = _fx.CreateCustomer();

        var act = () => _fx.CartService.RemoveFromCart(customer, 99999);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── UpdateQuantity ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateQuantity_ValidQuantity_UpdatesItem()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 10);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        _fx.CartService.UpdateQuantity(customer, product.Id, 5);

        customer.Cart.Items.Single(i => i.ProductId == product.Id).Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_ToZero_RemovesItem()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        _fx.CartService.UpdateQuantity(customer, product.Id, 0);

        customer.Cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateQuantity_ExceedsStock_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 3);
        _fx.CartService.AddToCart(customer, product.Id, 1);

        var act = () => _fx.CartService.UpdateQuantity(customer, product.Id, 10);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── ClearCart ──────────────────────────────────────────────────────────────

    [Fact]
    public void ClearCart_WithItems_EmptiesCart()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop", stock: 5);
        _fx.CartService.AddToCart(customer, product.Id, 2);

        _fx.CartService.ClearCart(customer);

        customer.Cart.IsEmpty.Should().BeTrue();
    }
}

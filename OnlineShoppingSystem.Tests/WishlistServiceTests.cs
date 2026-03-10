using FluentAssertions;
using Xunit;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for WishlistService: adding, removing, duplicate prevention,
/// and handling of soft-deleted products.
/// </summary>
public class WishlistServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── AddToWishlist ──────────────────────────────────────────────────────────

    [Fact]
    public void AddToWishlist_ValidProduct_AppearsInWishlist()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");

        _fx.WishlistService.AddToWishlist(customer, product.Id);

        customer.Wishlist.Should().Contain(product.Id);
    }

    [Fact]
    public void AddToWishlist_ValidProduct_AppearsInGetWishlist()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");

        _fx.WishlistService.AddToWishlist(customer, product.Id);

        _fx.WishlistService.GetWishlist(customer)
            .Should().ContainSingle(p => p.Id == product.Id);
    }

    [Fact]
    public void AddToWishlist_DuplicateProduct_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");
        _fx.WishlistService.AddToWishlist(customer, product.Id);

        var act = () => _fx.WishlistService.AddToWishlist(customer, product.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already in your wishlist*");
    }

    [Fact]
    public void AddToWishlist_UnknownProduct_Throws()
    {
        var customer = _fx.CreateCustomer();

        var act = () => _fx.WishlistService.AddToWishlist(customer, 99999);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void AddToWishlist_MultipleProducts_AllAppear()
    {
        var customer  = _fx.CreateCustomer();
        var productA  = _fx.CreateProduct("Laptop");
        var productB  = _fx.CreateProduct("Mouse");

        _fx.WishlistService.AddToWishlist(customer, productA.Id);
        _fx.WishlistService.AddToWishlist(customer, productB.Id);

        var wishlist = _fx.WishlistService.GetWishlist(customer);
        wishlist.Should().HaveCount(2);
        wishlist.Should().Contain(p => p.Id == productA.Id);
        wishlist.Should().Contain(p => p.Id == productB.Id);
    }

    // ── RemoveFromWishlist ─────────────────────────────────────────────────────

    [Fact]
    public void RemoveFromWishlist_ExistingProduct_RemovesItFromWishlist()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");
        _fx.WishlistService.AddToWishlist(customer, product.Id);

        _fx.WishlistService.RemoveFromWishlist(customer, product.Id);

        customer.Wishlist.Should().NotContain(product.Id);
    }

    [Fact]
    public void RemoveFromWishlist_ExistingProduct_NoLongerInGetWishlist()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");
        _fx.WishlistService.AddToWishlist(customer, product.Id);

        _fx.WishlistService.RemoveFromWishlist(customer, product.Id);

        _fx.WishlistService.GetWishlist(customer).Should().BeEmpty();
    }

    [Fact]
    public void RemoveFromWishlist_ProductNotInWishlist_Throws()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");

        var act = () => _fx.WishlistService.RemoveFromWishlist(customer, product.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not in your wishlist*");
    }

    // ── GetWishlist ────────────────────────────────────────────────────────────

    [Fact]
    public void GetWishlist_EmptyWishlist_ReturnsEmptyList()
    {
        var customer = _fx.CreateCustomer();

        _fx.WishlistService.GetWishlist(customer).Should().BeEmpty();
    }

    [Fact]
    public void GetWishlist_SoftDeletedProduct_IsOmitted()
    {
        var customer = _fx.CreateCustomer();
        var product  = _fx.CreateProduct("Laptop");
        _fx.WishlistService.AddToWishlist(customer, product.Id);

        // Soft-delete the product
        _fx.ProductService.Delete(product.Id);

        // The wishlist ID still exists but the resolved product list should be empty
        _fx.WishlistService.GetWishlist(customer).Should().BeEmpty();
    }

    [Fact]
    public void GetWishlist_IsOrderedAlphabetically()
    {
        var customer = _fx.CreateCustomer();
        var productC = _fx.CreateProduct("Zebra Pen");
        var productA = _fx.CreateProduct("Apple Watch");
        var productB = _fx.CreateProduct("Mouse Pad");

        _fx.WishlistService.AddToWishlist(customer, productC.Id);
        _fx.WishlistService.AddToWishlist(customer, productA.Id);
        _fx.WishlistService.AddToWishlist(customer, productB.Id);

        var wishlist = _fx.WishlistService.GetWishlist(customer);

        wishlist.Select(p => p.Name).Should().BeInAscendingOrder();
    }
}

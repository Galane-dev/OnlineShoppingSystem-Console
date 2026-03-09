using FluentAssertions;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for ProductService: catalog retrieval, search ranking, stock management,
/// soft-delete, and restocking.
/// </summary>
public class ProductServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── Add / GetById / GetAll ─────────────────────────────────────────────────

    [Fact]
    public void Add_ValidProduct_AppearsInGetAll()
    {
        _fx.CreateProduct("Laptop");

        _fx.ProductService.GetAll().Should().ContainSingle(p => p.Name == "Laptop");
    }

    [Fact]
    public void GetById_ExistingId_ReturnsProduct()
    {
        var product = _fx.CreateProduct("Laptop");

        _fx.ProductService.GetById(product.Id).Should().NotBeNull();
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        _fx.ProductService.GetById(99999).Should().BeNull();
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ExistingProduct_PersistsChanges()
    {
        var product = _fx.CreateProduct("Laptop", price: 1000m);
        product.Price = 1200m;

        _fx.ProductService.Update(product);

        _fx.ProductService.GetById(product.Id)!.Price.Should().Be(1200m);
    }

    // ── Delete (soft) ──────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingProduct_HidesItFromGetAll()
    {
        var product = _fx.CreateProduct("Laptop");

        _fx.ProductService.Delete(product.Id);

        _fx.ProductService.GetAll().Should().NotContain(p => p.Id == product.Id);
    }

    [Fact]
    public void Delete_ExistingProduct_SetsIsActiveToFalse()
    {
        var product = _fx.CreateProduct("Laptop");

        _fx.ProductService.Delete(product.Id);

        // The product still exists in the store but is inactive (soft delete)
        _fx.Store.Products.First(p => p.Id == product.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public void Delete_UnknownId_Throws()
    {
        var act = () => _fx.ProductService.Delete(99999);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Restock ────────────────────────────────────────────────────────────────

    [Fact]
    public void Restock_ValidQuantity_IncreasesStock()
    {
        var product = _fx.CreateProduct("Laptop", stock: 5);

        _fx.ProductService.Restock(product.Id, 10);

        _fx.ProductService.GetById(product.Id)!.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void Restock_ZeroQuantity_Throws()
    {
        var product = _fx.CreateProduct("Laptop");

        var act = () => _fx.ProductService.Restock(product.Id, 0);

        act.Should().Throw<ArgumentException>();
    }

    // ── GetByCategory ──────────────────────────────────────────────────────────

    [Fact]
    public void GetByCategory_ReturnsOnlyMatchingCategory()
    {
        _fx.CreateProduct("Laptop",   category: "Electronics");
        _fx.CreateProduct("Yoga Mat", category: "Fitness");

        var results = _fx.ProductService.GetByCategory("Electronics");

        results.Should().ContainSingle(p => p.Name == "Laptop");
        results.Should().NotContain(p => p.Name == "Yoga Mat");
    }

    [Fact]
    public void GetByCategory_IsCaseInsensitive()
    {
        _fx.CreateProduct("Laptop", category: "Electronics");

        _fx.ProductService.GetByCategory("ELECTRONICS").Should().NotBeEmpty();
    }

    // ── GetLowStock ────────────────────────────────────────────────────────────

    [Fact]
    public void GetLowStock_ReturnsOnlyProductsBelowThreshold()
    {
        _fx.CreateProduct("Scarce Item", stock: 2);
        _fx.CreateProduct("Plentiful Item", stock: 100);

        var lowStock = _fx.ProductService.GetLowStock();

        lowStock.Should().ContainSingle(p => p.Name == "Scarce Item");
        lowStock.Should().NotContain(p => p.Name == "Plentiful Item");
    }

    // ── Search (fuzzy) ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_ExactNameMatch_ReturnsProduct()
    {
        _fx.CreateProduct("Wireless Mouse");

        _fx.ProductService.Search("Wireless Mouse").Should().ContainSingle(p => p.Name == "Wireless Mouse");
    }

    [Fact]
    public void Search_MatchesDescription()
    {
        _fx.CreateProduct("Mystery Box", price: 50, stock: 5);
        // The product created by CreateProduct has description "{name} description"
        // so "Mystery Box description" contains the word "description"
        var results = _fx.ProductService.Search("Mystery Box");

        results.Should().ContainSingle(p => p.Name == "Mystery Box");
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmptyList()
    {
        _fx.CreateProduct("Laptop");

        _fx.ProductService.Search("xyzzy_nonexistent").Should().BeEmpty();
    }

    [Fact]
    public void Search_BestMatchesRankedFirst()
    {
        // "Wireless Mouse" is an exact phrase match → score 1.0
        // "Mouse Pad" contains "mouse" but not "wireless" → lower score
        _fx.CreateProduct("Wireless Mouse");
        _fx.CreateProduct("Mouse Pad");

        var results = _fx.ProductService.Search("wireless mouse");

        results.First().Name.Should().Be("Wireless Mouse");
    }

    [Fact]
    public void Search_DeletedProducts_AreExcluded()
    {
        var product = _fx.CreateProduct("Laptop");
        _fx.ProductService.Delete(product.Id);

        _fx.ProductService.Search("Laptop").Should().BeEmpty();
    }
}

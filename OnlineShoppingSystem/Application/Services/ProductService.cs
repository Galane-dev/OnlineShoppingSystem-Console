using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages product catalog CRUD operations and inventory queries using LINQ.
/// Depends on IProductRepository — never accesses DataStore directly.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly DataStore          _store; // needed only for NextProductId()

    public ProductService(IProductRepository products, DataStore store)
    {
        _products = products;
        _store    = store;
    }

    public Product? GetById(int id) =>
        _products.GetActive().FirstOrDefault(p => p.Id == id);

    public List<Product> GetAll() =>
        _products.GetActive()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToList();

    /// <summary>
    /// Searches active products by name and description using fuzzy matching.
    /// Results are ordered highest-score first so the most relevant items appear at the top.
    /// </summary>
    public List<Product> Search(string query) =>
        _products.GetActive()
            .Select(p => new
            {
                Product = p,
                Score   = Math.Max(
                    FuzzyMatcher.Score(p.Name,        query),
                    FuzzyMatcher.Score(p.Description, query))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Product)
            .ToList();

    public List<Product> GetByCategory(string category) =>
        _products.GetActive()
            .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();

    /// <summary>Returns all active products at or below the low-stock threshold.</summary>
    public List<Product> GetLowStock() =>
        _products.GetActive()
            .Where(p => p.IsLowStock)
            .OrderBy(p => p.StockQuantity)
            .ToList();

    public void Add(Product product)
    {
        product.Id        = _store.NextProductId();
        product.CreatedAt = DateTime.Now;
        _products.Add(product);
    }

    public void Update(Product product)
    {
        var existing = _products.GetById(product.Id)
            ?? throw new InvalidOperationException($"Product with ID {product.Id} not found.");

        existing.Name          = product.Name;
        existing.Description   = product.Description;
        existing.Category      = product.Category;
        existing.Price         = product.Price;
        existing.StockQuantity = product.StockQuantity;
        _products.Save();
    }

    /// <summary>Soft-deletes a product so historical order data remains intact.</summary>
    public void Delete(int productId)
    {
        var product = _products.GetById(productId)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        product.IsActive = false;
        _products.Save();
    }

    public void Restock(int productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Restock quantity must be greater than zero.");

        var product = _products.GetActive().FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        product.StockQuantity += quantity;
        _products.Save();
    }
}

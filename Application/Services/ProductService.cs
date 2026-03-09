using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages product catalog CRUD operations and inventory queries using LINQ.
/// </summary>
public class ProductService : IProductService
{
    private readonly DataStore _store;

    public ProductService(DataStore store)
    {
        _store = store;
    }

    public Product? GetById(int id) =>
        _store.Products.FirstOrDefault(p => p.Id == id && p.IsActive);

    public List<Product> GetAll() =>
        _store.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToList();

    /// <summary>
    /// Searches active products by name and description using fuzzy matching.
    /// Each field is scored independently and the higher score is taken,
    /// so a strong name match always outranks a weak description match.
    /// Results with a score of zero are excluded. Results are returned
    /// highest-score first so the most relevant items appear at the top.
    /// </summary>
    public List<Product> Search(string query)
    {
        return _store.Products
            .Where(p => p.IsActive)
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
    }

    public List<Product> GetByCategory(string category) =>
        _store.Products
            .Where(p => p.IsActive &&
                p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();

    /// <summary>Returns all active products at or below the low-stock threshold.</summary>
    public List<Product> GetLowStock() =>
        _store.Products
            .Where(p => p.IsActive && p.IsLowStock)
            .OrderBy(p => p.StockQuantity)
            .ToList();

    public void Add(Product product)
    {
        product.Id = _store.NextProductId();
        product.CreatedAt = DateTime.Now;
        _store.Products.Add(product);
        _store.Save();
    }

    public void Update(Product product)
    {
        var existing = _store.Products.FirstOrDefault(p => p.Id == product.Id)
            ?? throw new InvalidOperationException($"Product with ID {product.Id} not found.");

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Category = product.Category;
        existing.Price = product.Price;
        existing.StockQuantity = product.StockQuantity;
        _store.Save();
    }

    /// <summary>Soft-deletes a product so historical order data remains intact.</summary>
    public void Delete(int productId)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        product.IsActive = false;
        _store.Save();
    }

    public void Restock(int productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Restock quantity must be greater than zero.");

        var product = _store.Products.FirstOrDefault(p => p.Id == productId && p.IsActive)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found.");

        product.StockQuantity += quantity;
        _store.Save();
    }
}

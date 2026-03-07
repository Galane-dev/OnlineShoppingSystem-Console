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
    /// Searches products by name, description, or category — case-insensitive.
    /// </summary>
    public List<Product> Search(string query) =>
        _store.Products
            .Where(p => p.IsActive &&
                (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 p.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Name)
            .ToList();

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

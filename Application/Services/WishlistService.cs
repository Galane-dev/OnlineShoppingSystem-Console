using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages a customer's wishlist — adding, removing, and listing saved products.
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly DataStore _store;

    public WishlistService(DataStore store)
    {
        _store = store;
    }

    /// <summary>Adds a product to the customer's wishlist if not already present.</summary>
    public void AddToWishlist(Customer customer, int productId)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == productId && p.IsActive)
            ?? throw new InvalidOperationException("Product not found.");

        if (customer.Wishlist.Contains(productId))
            throw new InvalidOperationException($"'{product.Name}' is already in your wishlist.");

        customer.Wishlist.Add(productId);
        _store.Save();
    }

    /// <summary>Removes a product from the customer's wishlist.</summary>
    public void RemoveFromWishlist(Customer customer, int productId)
    {
        if (!customer.Wishlist.Contains(productId))
            throw new InvalidOperationException("That product is not in your wishlist.");

        customer.Wishlist.Remove(productId);
        _store.Save();
    }

    /// <summary>
    /// Returns the full Product objects for every ID in the customer's wishlist.
    /// Products that have since been deleted are silently omitted.
    /// </summary>
    public List<Product> GetWishlist(Customer customer) =>
        _store.Products
            .Where(p => customer.Wishlist.Contains(p.Id) && p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
}

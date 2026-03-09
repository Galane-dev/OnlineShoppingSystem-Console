using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages a customer's wishlist — adding, removing, and listing saved products.
/// Depends on IProductRepository — never accesses DataStore directly.
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly IProductRepository _products;

    public WishlistService(IProductRepository products)
    {
        _products = products;
    }

    /// <summary>Adds a product to the customer's wishlist if not already present.</summary>
    public void AddToWishlist(Customer customer, int productId)
    {
        var product = _products.GetActive().FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");

        if (customer.Wishlist.Contains(productId))
            throw new InvalidOperationException($"'{product.Name}' is already in your wishlist.");

        customer.Wishlist.Add(productId);
        _products.Save();
    }

    /// <summary>Removes a product from the customer's wishlist.</summary>
    public void RemoveFromWishlist(Customer customer, int productId)
    {
        if (!customer.Wishlist.Contains(productId))
            throw new InvalidOperationException("That product is not in your wishlist.");

        customer.Wishlist.Remove(productId);
        _products.Save();
    }

    /// <summary>
    /// Returns the full Product objects for every ID in the customer's wishlist.
    /// Products that have since been soft-deleted are silently omitted.
    /// </summary>
    public List<Product> GetWishlist(Customer customer) =>
        _products.GetActive()
            .Where(p => customer.Wishlist.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToList();
}

using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages shopping cart operations, enforcing stock validation before cart mutations.
/// Depends on IProductRepository — never accesses DataStore directly.
/// </summary>
public class CartService : ICartService
{
    private readonly IProductRepository _products;

    public CartService(IProductRepository products)
    {
        _products = products;
    }

    /// <summary>Adds a product to the cart after verifying sufficient stock exists.</summary>
    public void AddToCart(Customer customer, int productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var product = _products.GetActive().FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");

        if (!product.IsInStock)
            throw new InvalidOperationException($"'{product.Name}' is out of stock.");

        var alreadyInCart = customer.Cart.Items
            .FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;

        if (alreadyInCart + quantity > product.StockQuantity)
            throw new InvalidOperationException(
                $"Only {product.StockQuantity - alreadyInCart} unit(s) available for '{product.Name}'.");

        customer.Cart.AddItem(product, quantity);
    }

    public void RemoveFromCart(Customer customer, int productId)
    {
        if (!customer.Cart.RemoveItem(productId))
            throw new InvalidOperationException("Item not found in cart.");
    }

    /// <summary>Updates quantity of an existing cart item; removes it if quantity is zero.</summary>
    public void UpdateQuantity(Customer customer, int productId, int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        var item = customer.Cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException("Item not found in cart.");

        if (quantity == 0)
        {
            customer.Cart.RemoveItem(productId);
            return;
        }

        var product = _products.GetById(productId)!;
        if (quantity > product.StockQuantity)
            throw new InvalidOperationException($"Only {product.StockQuantity} unit(s) available.");

        item.Quantity = quantity;
    }

    public void ClearCart(Customer customer) => customer.Cart.Clear();

    /// <summary>Renders a formatted cart summary to the console.</summary>
    public void DisplayCart(Customer customer)
    {
        if (customer.Cart.IsEmpty)
        {
            ConsoleHelper.WriteWarning("Your cart is empty.");
            return;
        }

        ConsoleHelper.WriteHeader("Shopping Cart");
        Console.WriteLine($"{"Item",-30} {"Qty",5} {"Unit Price",12} {"Total",12}");
        Console.WriteLine(new string('-', 62));

        foreach (var item in customer.Cart.Items)
            Console.WriteLine($"{item.ProductName,-30} {item.Quantity,5} R{item.UnitPrice,10:F2} R{item.LineTotal,10:F2}");

        Console.WriteLine(new string('-', 62));
        Console.WriteLine($"{"TOTAL",-47} R{customer.Cart.Total,10:F2}");
    }
}

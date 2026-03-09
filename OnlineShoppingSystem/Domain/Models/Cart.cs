namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents a single line item in a customer's shopping cart.
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;

    public override string ToString() =>
        $"{ProductName} x{Quantity} @ R{UnitPrice:F2} = R{LineTotal:F2}";
}

/// <summary>
/// Represents a customer's active shopping cart containing items pending checkout.
/// </summary>
public class Cart
{
    public int CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public decimal Total => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
    public bool IsEmpty => !Items.Any();

    /// <summary>Adds a product to the cart or increments quantity if already present.</summary>
    public void AddItem(Product product, int quantity)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        LastUpdated = DateTime.Now;
    }

    /// <summary>Removes a cart item by product ID.</summary>
    public bool RemoveItem(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return false;
        Items.Remove(item);
        LastUpdated = DateTime.Now;
        return true;
    }

    /// <summary>Clears all items from the cart.</summary>
    public void Clear()
    {
        Items.Clear();
        LastUpdated = DateTime.Now;
    }
}

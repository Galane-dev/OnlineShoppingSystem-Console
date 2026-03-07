namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents a customer who can browse products, manage a cart, and place orders.
/// </summary>
public class Customer : User
{
    public override string Role => "Customer";

    public decimal WalletBalance { get; set; } = 0;
    public Cart Cart { get; set; } = new Cart();
    public List<Order> OrderHistory { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();

    public Customer()
    {
        Cart = new Cart { CustomerId = Id };
    }
}

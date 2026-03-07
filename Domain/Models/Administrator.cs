namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents an administrator who manages products, inventory, and orders.
/// </summary>
public class Administrator : User
{
    public override string Role => "Administrator";
    public string Department { get; set; } = "General";
}

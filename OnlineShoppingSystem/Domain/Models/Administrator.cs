namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents an administrator who manages products, inventory, and orders.
/// New admins start as unapproved and cannot access the dashboard until an
/// existing admin approves their account.
/// </summary>
public class Administrator : User
{
    public override string Role => "Administrator";

    public string Department { get; set; } = "General";

    /// <summary>
    /// False for newly registered admins pending approval.
    /// Seeded admins are approved by default.
    /// </summary>
    public bool IsApproved { get; set; } = false;
}

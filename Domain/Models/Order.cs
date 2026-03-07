namespace OnlineShoppingSystem.Domain.Models;

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Represents a line item within a placed order, capturing a snapshot of price at time of purchase.
/// </summary>
public class OrderItem
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
/// Represents a completed customer order including items, payment, and fulfilment status.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.Now;
    public DateTime? LastUpdated { get; set; }

    public override string ToString() =>
        $"Order #{Id} | {CustomerName} | R{TotalAmount:F2} | {Status} | {PlacedAt:dd MMM yyyy HH:mm}";
}

namespace OnlineShoppingSystem.Domain.Models;

public enum PaymentStatus
{
    Success,
    Failed,
    Refunded
}

/// <summary>
/// Represents a payment transaction linked to an order, processed through the wallet system.
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.Now;

    public override string ToString() =>
        $"Payment #{Id} | Order #{OrderId} | R{Amount:F2} | {Status} | {ProcessedAt:dd MMM yyyy HH:mm}";
}

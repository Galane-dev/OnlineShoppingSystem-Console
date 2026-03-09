namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents a customer product review containing a star rating and written comment.
/// </summary>
public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Rating must be between 1 and 5 inclusive.</summary>
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public override string ToString() =>
        $"[{new string('★', Rating)}{new string('☆', 5 - Rating)}] {CustomerName}: {Comment} ({CreatedAt:dd MMM yyyy})";
}

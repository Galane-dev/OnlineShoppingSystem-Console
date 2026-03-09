namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Represents a product available in the store catalog.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Review> Reviews { get; set; } = new();

    // Low stock threshold used for admin alerts
    public const int LowStockThreshold = 5;

    /// <summary>Calculates the average rating from all submitted reviews.</summary>
    public double AverageRating =>
        Reviews.Count > 0 ? Reviews.Average(r => r.Rating) : 0;

    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool IsInStock => StockQuantity > 0;

    public override string ToString() =>
        $"[{Id}] {Name} | {Category} | R{Price:F2} | Stock: {StockQuantity} | Rating: {AverageRating:F1}/5";
}

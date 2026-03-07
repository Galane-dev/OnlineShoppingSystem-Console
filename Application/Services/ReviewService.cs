using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages product reviews, enforcing one-review-per-order-item business rule.
/// </summary>
public class ReviewService
{
    private readonly DataStore _store;

    public ReviewService(DataStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Submits a review for a product. Customers may only review products they have purchased.
    /// </summary>
    public Review SubmitReview(Customer customer, int productId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var product = _store.Products.FirstOrDefault(p => p.Id == productId && p.IsActive)
            ?? throw new InvalidOperationException("Product not found.");

        // Guard: customer must have purchased this product
        var hasPurchased = customer.OrderHistory
            .Any(o => o.Status != OrderStatus.Cancelled &&
                      o.Items.Any(i => i.ProductId == productId));

        if (!hasPurchased)
            throw new InvalidOperationException(
                "You can only review products you have purchased.");

        // Guard: prevent duplicate reviews for the same product
        var alreadyReviewed = product.Reviews
            .Any(r => r.CustomerId == customer.Id);

        if (alreadyReviewed)
            throw new InvalidOperationException(
                "You have already submitted a review for this product.");

        var review = new Review
        {
            Id = _store.NextReviewId(),
            ProductId = productId,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.Now
        };

        product.Reviews.Add(review);
        customer.Reviews.Add(review);
        _store.Save();
        return review;
    }

    public List<Review> GetReviewsForProduct(int productId) =>
        _store.Products
            .Where(p => p.Id == productId)
            .SelectMany(p => p.Reviews)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
}

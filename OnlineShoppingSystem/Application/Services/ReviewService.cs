using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Manages product reviews, enforcing the delivered-order and one-review-per-product rules.
/// Depends on IProductRepository — never accesses DataStore directly.
/// </summary>
public class ReviewService
{
    private readonly IProductRepository _products;
    private readonly DataStore          _store; // needed only for NextReviewId()

    public ReviewService(IProductRepository products, DataStore store)
    {
        _products = products;
        _store    = store;
    }

    /// <summary>
    /// Submits a review. The customer must have a delivered order containing
    /// the product, and may only review each product once.
    /// </summary>
    public Review SubmitReview(Customer customer, int productId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var product = _products.GetActive().FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");

        var hasDeliveredOrder = customer.OrderHistory
            .Any(o => o.Status == OrderStatus.Delivered &&
                      o.Items.Any(i => i.ProductId == productId));

        if (!hasDeliveredOrder)
            throw new InvalidOperationException(
                "You can only review a product once your order has been delivered.");

        if (product.Reviews.Any(r => r.CustomerId == customer.Id))
            throw new InvalidOperationException(
                "You have already submitted a review for this product.");

        var review = new Review
        {
            Id           = _store.NextReviewId(),
            ProductId    = productId,
            CustomerId   = customer.Id,
            CustomerName = customer.FullName,
            Rating       = rating,
            Comment      = comment,
            CreatedAt    = DateTime.Now
        };

        product.Reviews.Add(review);
        customer.Reviews.Add(review);
        _products.Save();
        return review;
    }

    public List<Review> GetReviewsForProduct(int productId) =>
        _products.GetAll()
            .Where(p => p.Id == productId)
            .SelectMany(p => p.Reviews)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
}

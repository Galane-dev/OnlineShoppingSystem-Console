using FluentAssertions;
using OnlineShoppingSystem.Domain.Models;
using Xunit;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for ReviewService: submission, delivered-order guard,
/// duplicate prevention, and rating validation.
/// </summary>
public class ReviewServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── SubmitReview ───────────────────────────────────────────────────────────

    [Fact]
    public void SubmitReview_DeliveredOrder_CreatesReview()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        var review = _fx.ReviewService.SubmitReview(customer, product.Id, 5, "Great product!");

        review.Should().NotBeNull();
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Great product!");
        review.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public void SubmitReview_DeliveredOrder_AppearsOnProduct()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        _fx.ReviewService.SubmitReview(customer, product.Id, 4, "Good.");

        product.Reviews.Should().ContainSingle(r => r.CustomerId == customer.Id);
    }

    [Fact]
    public void SubmitReview_DeliveredOrder_AppearsOnCustomer()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        _fx.ReviewService.SubmitReview(customer, product.Id, 4, "Good.");

        customer.Reviews.Should().ContainSingle(r => r.ProductId == product.Id);
    }

    [Fact]
    public void SubmitReview_WithoutDeliveredOrder_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);

        // No order at all — should be rejected
        var act = () => _fx.ReviewService.SubmitReview(customer, product.Id, 5, "Nice.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*delivered*");
    }

    [Fact]
    public void SubmitReview_PendingOrderOnly_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);

        // Place order but leave it in Pending — not yet delivered
        _fx.CartService.AddToCart(customer, product.Id, 1);
        var order = _fx.OrderService.PlaceOrder(customer);
        customer.OrderHistory.Add(order);

        var act = () => _fx.ReviewService.SubmitReview(customer, product.Id, 5, "Nice.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*delivered*");
    }

    [Fact]
    public void SubmitReview_DuplicateReview_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        _fx.ReviewService.SubmitReview(customer, product.Id, 5, "Great!");

        var act = () => _fx.ReviewService.SubmitReview(customer, product.Id, 4, "Again.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already submitted*");
    }

    [Fact]
    public void SubmitReview_UnknownProduct_Throws()
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);

        var act = () => _fx.ReviewService.SubmitReview(customer, 99999, 5, "Nice.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Rating validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void SubmitReview_InvalidRating_Throws(int invalidRating)
    {
        var customer = _fx.CreateCustomer(walletBalance: 500m);
        var product  = _fx.CreateProduct("Laptop", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        var act = () => _fx.ReviewService.SubmitReview(customer, product.Id, invalidRating, "Test.");

        act.Should().Throw<ArgumentException>().WithMessage("*between 1 and 5*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void SubmitReview_ValidRating_Succeeds(int validRating)
    {
        var customer = _fx.CreateCustomer($"user{validRating}", walletBalance: 500m);
        var product  = _fx.CreateProduct($"Product{validRating}", price: 100m, stock: 5);
        _fx.CreateDeliveredOrder(customer, product);

        var act = () => _fx.ReviewService.SubmitReview(customer, product.Id, validRating, "Test.");

        act.Should().NotThrow();
    }

    // ── GetReviewsForProduct ───────────────────────────────────────────────────

    [Fact]
    public void GetReviewsForProduct_MultipleReviewers_ReturnsAllReviews()
    {
        var alice   = _fx.CreateCustomer("alice", walletBalance: 500m);
        var bob     = _fx.CreateCustomer("bob",   walletBalance: 500m);
        var product = _fx.CreateProduct("Laptop", price: 100m, stock: 10);

        _fx.CreateDeliveredOrder(alice, product);
        _fx.CreateDeliveredOrder(bob,   product);

        _fx.ReviewService.SubmitReview(alice, product.Id, 5, "Excellent!");
        _fx.ReviewService.SubmitReview(bob,   product.Id, 3, "Decent.");

        var reviews = _fx.ReviewService.GetReviewsForProduct(product.Id);

        reviews.Should().HaveCount(2);
    }
}

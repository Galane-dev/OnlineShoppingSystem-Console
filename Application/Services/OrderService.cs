using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Handles order creation, status management, and order history queries.
/// </summary>
public class OrderService : IOrderService
{
    private readonly DataStore _store;
    private readonly IPaymentService _paymentService;

    public OrderService(DataStore store, IPaymentService paymentService)
    {
        _store = store;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Creates an order from the customer's cart, processes payment, and decrements stock.
    /// Rolls back stock changes if payment fails.
    /// </summary>
    public Order PlaceOrder(Customer customer)
    {
        if (customer.Cart.IsEmpty)
            throw new InvalidOperationException("Cannot place an order with an empty cart.");

        ValidateStockAvailability(customer.Cart);

        var order = BuildOrder(customer);
        _store.Orders.Add(order);
        customer.OrderHistory.Add(order);

        // Attempt payment — if it fails, the order is marked cancelled
        try
        {
            _paymentService.ProcessPayment(customer, order.TotalAmount, order.Id);
            DecrementStock(customer.Cart);
            customer.Cart.Clear();
            _store.Save();
        }
        catch
        {
            order.Status = OrderStatus.Cancelled;
            _store.Save();
            throw;
        }

        return order;
    }

    public Order? GetById(int orderId) =>
        _store.Orders.FirstOrDefault(o => o.Id == orderId);

    public List<Order> GetAllOrders() =>
        _store.Orders.OrderByDescending(o => o.PlacedAt).ToList();

    public List<Order> GetOrdersByCustomer(int customerId) =>
        _store.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

    public void UpdateStatus(int orderId, OrderStatus status)
    {
        var order = _store.Orders.FirstOrDefault(o => o.Id == orderId)
            ?? throw new InvalidOperationException($"Order #{orderId} not found.");

        order.Status      = status;
        order.LastUpdated = DateTime.Now;
        _store.Save();
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    /// <summary>Ensures every cart item has sufficient stock before committing the order.</summary>
    private void ValidateStockAvailability(Cart cart)
    {
        var stockIssues = cart.Items
            .Select(item => new
            {
                item,
                product = _store.Products.FirstOrDefault(p => p.Id == item.ProductId)
            })
            .Where(x => x.product == null || x.product.StockQuantity < x.item.Quantity)
            .Select(x => x.product == null
                ? $"Product ID {x.item.ProductId} no longer exists."
                : $"'{x.product.Name}' only has {x.product.StockQuantity} unit(s) in stock.")
            .ToList();

        if (stockIssues.Any())
            throw new InvalidOperationException(
                $"Stock issues:\n{string.Join("\n", stockIssues)}");
    }

    private Order BuildOrder(Customer customer)
    {
        var items = customer.Cart.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList();

        return new Order
        {
            Id = _store.NextOrderId(),
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            Items = items,
            TotalAmount = customer.Cart.Total,
            Status = OrderStatus.Pending,
            PlacedAt = DateTime.Now
        };
    }

    private void DecrementStock(Cart cart)
    {
        foreach (var item in cart.Items)
        {
            var product = _store.Products.First(p => p.Id == item.ProductId);
            product.StockQuantity -= item.Quantity;
        }
    }
}

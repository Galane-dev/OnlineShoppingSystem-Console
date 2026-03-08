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

    /// <summary>
    /// Cancels an order that is still Pending or Processing.
    /// Refunds the full order amount to the customer's wallet.
    /// Shipped or Delivered orders must use ReturnOrder instead.
    /// </summary>
    public void CancelOrder(Customer customer, int orderId)
    {
        var order = GetCustomerOrder(customer, orderId);

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException(
                $"Order #{orderId} has already been {order.Status.ToString().ToLower()} and cannot be cancelled. Use 'Return Order' instead.");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Order #{orderId} is already cancelled.");

        order.Status      = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.Now;
        order.LastUpdated = DateTime.Now;

        customer.WalletBalance += order.TotalAmount;
        _store.Save();
    }

    /// <summary>
    /// Returns a Delivered order, restocking all items and refunding the customer's wallet.
    /// Only Delivered orders are eligible for return.
    /// </summary>
    public void ReturnOrder(Customer customer, int orderId)
    {
        var order = GetCustomerOrder(customer, orderId);

        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException(
                $"Only delivered orders can be returned. Order #{orderId} is currently {order.Status.ToString().ToLower()}.");

        RestockOrderItems(order);

        order.Status      = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.Now;
        order.LastUpdated = DateTime.Now;

        customer.WalletBalance += order.TotalAmount;
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

    /// <summary>Finds an order that belongs to the given customer, throwing if not found.</summary>
    private Order GetCustomerOrder(Customer customer, int orderId)
    {
        var order = _store.Orders.FirstOrDefault(o => o.Id == orderId && o.CustomerId == customer.Id)
            ?? throw new InvalidOperationException($"Order #{orderId} was not found in your order history.");

        return order;
    }

    /// <summary>Returns each order item's quantity back to the product's stock.</summary>
    private void RestockOrderItems(Order order)
    {
        foreach (var item in order.Items)
        {
            var product = _store.Products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
                product.StockQuantity += item.Quantity;
        }
    }
}

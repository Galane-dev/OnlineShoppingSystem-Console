using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Handles order creation, status management, and order history queries.
/// Depends on IOrderRepository and IProductRepository — never accesses DataStore directly.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository   _orders;
    private readonly IProductRepository _products;
    private readonly IPaymentService    _paymentService;
    private readonly DataStore          _store; // needed only for NextOrderId()

    public OrderService(IOrderRepository orders, IProductRepository products,
                        IPaymentService paymentService, DataStore store)
    {
        _orders         = orders;
        _products       = products;
        _paymentService = paymentService;
        _store          = store;
    }

    /// <summary>
    /// Creates an order from the customer's cart, processes payment, and decrements stock.
    /// Rolls back to Cancelled status if payment fails.
    /// </summary>
    public Order PlaceOrder(Customer customer)
    {
        if (customer.Cart.IsEmpty)
            throw new InvalidOperationException("Cannot place an order with an empty cart.");

        ValidateStockAvailability(customer.Cart);

        var order = BuildOrder(customer);
        _orders.Add(order);
        customer.OrderHistory.Add(order);

        try
        {
            _paymentService.ProcessPayment(customer, order.TotalAmount, order.Id);
            DecrementStock(customer.Cart);
            customer.Cart.Clear();
            _orders.Save();
        }
        catch
        {
            order.Status = OrderStatus.Cancelled;
            _orders.Save();
            throw;
        }

        return order;
    }

    public Order? GetById(int orderId) => _orders.GetById(orderId);

    public List<Order> GetAllOrders() => _orders.GetAll();

    public List<Order> GetOrdersByCustomer(int customerId) =>
        _orders.GetByCustomer(customerId);

    public void UpdateStatus(int orderId, OrderStatus status)
    {
        var order = _orders.GetById(orderId)
            ?? throw new InvalidOperationException($"Order #{orderId} not found.");

        order.Status      = status;
        order.LastUpdated = DateTime.Now;
        _orders.Save();
    }

    /// <summary>
    /// Cancels a Pending or Processing order and refunds the customer's wallet.
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
        _orders.Save();
    }

    /// <summary>
    /// Returns a Delivered order, restocking all items and refunding the customer's wallet.
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
        _orders.Save();
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private void ValidateStockAvailability(Cart cart)
    {
        var stockIssues = cart.Items
            .Select(item => new
            {
                item,
                product = _products.GetById(item.ProductId)
            })
            .Where(x => x.product == null || x.product.StockQuantity < x.item.Quantity)
            .Select(x => x.product == null
                ? $"Product ID {x.item.ProductId} no longer exists."
                : $"'{x.product.Name}' only has {x.product.StockQuantity} unit(s) in stock.")
            .ToList();

        if (stockIssues.Any())
            throw new InvalidOperationException($"Stock issues:\n{string.Join("\n", stockIssues)}");
    }

    private Order BuildOrder(Customer customer) => new()
    {
        Id           = _store.NextOrderId(),
        CustomerId   = customer.Id,
        CustomerName = customer.FullName,
        Items        = customer.Cart.Items.Select(i => new OrderItem
        {
            ProductId   = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice   = i.UnitPrice,
            Quantity    = i.Quantity
        }).ToList(),
        TotalAmount = customer.Cart.Total,
        Status      = OrderStatus.Pending,
        PlacedAt    = DateTime.Now
    };

    private void DecrementStock(Cart cart)
    {
        foreach (var item in cart.Items)
        {
            var product = _products.GetById(item.ProductId)!;
            product.StockQuantity -= item.Quantity;
        }
    }

    private Order GetCustomerOrder(Customer customer, int orderId) =>
        _orders.GetByCustomer(customer.Id).FirstOrDefault(o => o.Id == orderId)
        ?? throw new InvalidOperationException($"Order #{orderId} was not found in your order history.");

    private void RestockOrderItems(Order order)
    {
        foreach (var item in order.Items)
        {
            var product = _products.GetById(item.ProductId);
            if (product != null)
                product.StockQuantity += item.Quantity;
        }
        _products.Save();
    }
}

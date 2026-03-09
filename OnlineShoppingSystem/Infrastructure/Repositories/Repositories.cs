using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Infrastructure.Repositories;

/// <summary>Concrete repository for Products backed by the in-memory DataStore.</summary>
public class ProductRepository : IProductRepository
{
    private readonly DataStore _store;

    public ProductRepository(DataStore store)
    {
        _store = store;
    }

    public Product? GetById(int id) =>
        _store.Products.FirstOrDefault(p => p.Id == id);

    public List<Product> GetAll() => _store.Products.ToList();

    /// <summary>Returns only products that have not been soft-deleted.</summary>
    public List<Product> GetActive() =>
        _store.Products.Where(p => p.IsActive).ToList();

    public void Add(Product product)
    {
        _store.Products.Add(product);
        _store.Save();
    }

    public void Save() => _store.Save();
}

/// <summary>Concrete repository for Orders backed by the in-memory DataStore.</summary>
public class OrderRepository : IOrderRepository
{
    private readonly DataStore _store;

    public OrderRepository(DataStore store)
    {
        _store = store;
    }

    public Order? GetById(int id) =>
        _store.Orders.FirstOrDefault(o => o.Id == id);

    public List<Order> GetAll() =>
        _store.Orders.OrderByDescending(o => o.PlacedAt).ToList();

    public List<Order> GetByCustomer(int customerId) =>
        _store.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

    public void Add(Order order)
    {
        _store.Orders.Add(order);
        _store.Save();
    }

    public void Save() => _store.Save();
}

/// <summary>Concrete repository for Payments backed by the in-memory DataStore.</summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly DataStore _store;

    public PaymentRepository(DataStore store)
    {
        _store = store;
    }

    public List<Payment> GetByCustomer(int customerId) =>
        _store.Payments
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.ProcessedAt)
            .ToList();

    public void Add(Payment payment)
    {
        _store.Payments.Add(payment);
        _store.Save();
    }

    public void Save() => _store.Save();
}

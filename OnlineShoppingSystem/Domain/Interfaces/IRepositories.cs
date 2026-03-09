using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Domain.Interfaces;

/// <summary>
/// Defines read/write access to the User collection.
/// Services depend on this interface; the concrete implementation lives in Infrastructure.
/// </summary>
public interface IUserRepository
{
    User?          GetById(int id);
    User?          GetByUsername(string username);
    List<User>     GetAll();
    List<Administrator> GetPendingAdmins();
    void           Add(User user);
    void           Save();
}

/// <summary>Defines read/write access to the Product collection.</summary>
public interface IProductRepository
{
    Product?       GetById(int id);
    List<Product>  GetAll();
    List<Product>  GetActive();
    void           Add(Product product);
    void           Save();
}

/// <summary>Defines read/write access to the Order collection.</summary>
public interface IOrderRepository
{
    Order?         GetById(int id);
    List<Order>    GetAll();
    List<Order>    GetByCustomer(int customerId);
    void           Add(Order order);
    void           Save();
}

/// <summary>Defines read/write access to the Payment collection.</summary>
public interface IPaymentRepository
{
    List<Payment>  GetByCustomer(int customerId);
    void           Add(Payment payment);
    void           Save();
}

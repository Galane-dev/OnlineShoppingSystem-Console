using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Domain.Interfaces;

/// <summary>Defines product catalog management operations.</summary>
public interface IProductService
{
    Product? GetById(int id);
    List<Product> GetAll();
    List<Product> Search(string query);
    List<Product> GetByCategory(string category);
    List<Product> GetLowStock();
    void Add(Product product);
    void Update(Product product);
    void Delete(int productId);
    void Restock(int productId, int quantity);
}

/// <summary>Defines shopping cart operations for a customer session.</summary>
public interface ICartService
{
    void AddToCart(Customer customer, int productId, int quantity);
    void RemoveFromCart(Customer customer, int productId);
    void UpdateQuantity(Customer customer, int productId, int quantity);
    void ClearCart(Customer customer);
    void DisplayCart(Customer customer);
}

/// <summary>Defines order lifecycle operations.</summary>
public interface IOrderService
{
    Order PlaceOrder(Customer customer);
    Order? GetById(int orderId);
    List<Order> GetAllOrders();
    List<Order> GetOrdersByCustomer(int customerId);
    void UpdateStatus(int orderId, OrderStatus status);
}

/// <summary>Defines wallet and payment processing operations.</summary>
public interface IPaymentService
{
    void AddFunds(Customer customer, decimal amount);
    Payment ProcessPayment(Customer customer, decimal amount, int orderId);
    List<Payment> GetPaymentHistory(int customerId);
}

/// <summary>Defines user authentication and registration operations.</summary>
public interface IAuthService
{
    User? Login(string username, string password);
    Customer RegisterCustomer(string username, string email, string password, string fullName);
    bool UsernameExists(string username);
}

/// <summary>Defines reporting and analytics operations for administrators.</summary>
public interface IReportService
{
    void GenerateSalesReport();
    void GenerateTopProductsReport();
    void GenerateLowStockReport();
    void GenerateCustomerOrderReport();
}

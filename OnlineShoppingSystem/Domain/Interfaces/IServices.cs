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

    /// <summary>Cancels a Pending or Processing order and refunds the customer's wallet.</summary>
    void CancelOrder(Customer customer, int orderId);

    /// <summary>Returns a Delivered order, restocks items, and refunds the customer's wallet.</summary>
    void ReturnOrder(Customer customer, int orderId);
}

/// <summary>Defines wishlist management operations.</summary>
public interface IWishlistService
{
    void AddToWishlist(Customer customer, int productId);
    void RemoveFromWishlist(Customer customer, int productId);
    List<Product> GetWishlist(Customer customer);
}

/// <summary>Defines wallet and payment processing operations.</summary>
public interface IPaymentService
{
    void AddFunds(Customer customer, decimal amount);
    Payment ProcessPayment(Customer customer, decimal amount, int orderId);
    List<Payment> GetPaymentHistory(int customerId);
}

/// <summary>Defines user authentication, registration, and account management operations.</summary>
public interface IAuthService
{
    User? Login(string username, string password);

    /// <summary>Registers a new customer, storing a hashed security answer for password reset.</summary>
    Customer RegisterCustomer(string username, string email, string password, string fullName,
                              string securityQuestion, string securityAnswer);

    /// <summary>
    /// Registers a new administrator in a pending (unapproved) state.
    /// The account cannot log in until an existing admin approves it.
    /// </summary>
    Administrator RegisterAdmin(string username, string email, string password, string fullName,
                                string department, string securityQuestion, string securityAnswer);

    /// <summary>Returns all administrator accounts awaiting approval.</summary>
    List<Administrator> GetPendingAdmins();

    /// <summary>Approves a pending admin account so the user can log in.</summary>
    void ApproveAdmin(int adminId);

    bool UsernameExists(string username);

    /// <summary>Returns the User with the given username, or null if not found.</summary>
    User? FindByUsername(string username);

    /// <summary>Updates the user's display name.</summary>
    void UpdateFullName(User user, string newFullName);

    /// <summary>Changes the user's password after verifying the current one.</summary>
    void ChangePassword(User user, string currentPassword, string newPassword);

    /// <summary>Resets the password after verifying the security answer.</summary>
    void ResetPassword(string username, string securityAnswer, string newPassword);
}

/// <summary>Defines reporting and analytics operations for administrators.</summary>
public interface IReportService
{
    void GenerateSalesReport();
    void GenerateTopProductsReport();
    void GenerateLowStockReport();
    void GenerateCustomerOrderReport();
}

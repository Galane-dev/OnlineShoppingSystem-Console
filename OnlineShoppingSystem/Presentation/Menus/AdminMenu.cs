using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Application.Session;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Drives all administrator interactions: product management, order processing,
/// inventory control, report generation, and admin account approval.
/// </summary>
public class AdminMenu
{
    private readonly ProductService _productService;
    private readonly OrderService   _orderService;
    private readonly ReportService  _reportService;
    private readonly AuthService    _authService;

    public AdminMenu(
        ProductService productService,
        OrderService   orderService,
        ReportService  reportService,
        AuthService    authService)
    {
        _productService = productService;
        _orderService   = orderService;
        _reportService  = reportService;
        _authService    = authService;
    }

    public void Run(Administrator admin)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader($"Admin Menu  ·  {admin.FullName}");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ── Product Management ──────────────────────");
            Console.ResetColor();
            ConsoleHelper.WriteMenuOption(1, "Add Product",      "Add a new product to the catalog");
            ConsoleHelper.WriteMenuOption(2, "Update Product",   "Edit an existing product's details");
            ConsoleHelper.WriteMenuOption(3, "Delete Product",   "Remove a product from the catalog");
            ConsoleHelper.WriteMenuOption(4, "Restock Product",  "Increase stock for a product");
            ConsoleHelper.WriteMenuOption(5, "View All Products","Full product table with stock levels");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ── Order Management ────────────────────────");
            Console.ResetColor();
            ConsoleHelper.WriteMenuOption(6, "View All Orders",  "See every order placed");
            ConsoleHelper.WriteMenuOption(7, "Update Order Status", "Change the fulfilment status");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ── Reports ─────────────────────────────────");
            Console.ResetColor();
            ConsoleHelper.WriteMenuOption(8,  "Low Stock Report",       "Products needing restocking");
            ConsoleHelper.WriteMenuOption(9,  "Sales Report",           "Revenue and order summaries");
            ConsoleHelper.WriteMenuOption(10, "Top Products Report",    "Best-selling products");
            ConsoleHelper.WriteMenuOption(11, "Customer Order Report",  "Spend per customer");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ── Admin Management ─────────────────────────");
            Console.ResetColor();

            var pendingCount = _authService.GetPendingAdmins().Count;
            var pendingLabel = pendingCount > 0 ? $"Approve Admins  ({pendingCount} pending)" : "Approve Admins";
            ConsoleHelper.WriteMenuOption(12, pendingLabel, "Review and approve new admin registrations");

            ConsoleHelper.WriteMenuOption(0, "Logout");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 12);

            switch (choice)
            {
                case 1:  AddProduct();                                                         break;
                case 2:  UpdateProduct();                                                      break;
                case 3:  DeleteProduct();                                                      break;
                case 4:  RestockProduct();                                                     break;
                case 5:  ViewAllProducts();                                                    break;
                case 6:  ViewAllOrders();                                                      break;
                case 7:  UpdateOrderStatus();                                                  break;
                case 8:  _reportService.GenerateLowStockReport();    ConsoleHelper.PressEnterToContinue(); break;
                case 9:  _reportService.GenerateSalesReport();       ConsoleHelper.PressEnterToContinue(); break;
                case 10: _reportService.GenerateTopProductsReport(); ConsoleHelper.PressEnterToContinue(); break;
                case 11: _reportService.GenerateCustomerOrderReport(); ConsoleHelper.PressEnterToContinue(); break;
                case 12: ApproveAdmins();                                                      break;
                case 0:
                    ConsoleHelper.WriteInfo("Logged out.");
                    ConsoleHelper.PressEnterToContinue();
                    return;
            }
        }
    }

    // ── Product Management ────────────────────────────────────────────────────

    private void AddProduct()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Add New Product");

        try
        {
            var product = new Product
            {
                Name          = ConsoleHelper.ReadRequiredInput("Name"),
                Description   = ConsoleHelper.ReadRequiredInput("Description"),
                Category      = ConsoleHelper.ReadRequiredInput("Category"),
                Price         = ConsoleHelper.ReadDecimal("Price (R)", 0.01m),
                StockQuantity = ConsoleHelper.ReadInt("Initial Stock Quantity", 0)
            };

            _productService.Add(product);
            Console.WriteLine();
            ConsoleHelper.WriteSuccess($"Product '{product.Name}' added with ID #{product.Id}.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    /// <summary>
    /// Shows all products first so the admin can see available IDs,
    /// then prompts for the one to update.
    /// </summary>
    private void UpdateProduct()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Update Product");

        var products = _productService.GetAll();
        if (!products.Any())
        {
            ConsoleHelper.WriteWarning("No products in catalog.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintProductTable(products);
        Console.WriteLine();

        var id      = ConsoleHelper.ReadInt("Enter Product ID to update (0 to cancel)", 0);
        if (id == 0) return;

        var product = _productService.GetById(id);
        if (product == null)
        {
            ConsoleHelper.WriteError("Product not found.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteInfo("Leave a field blank to keep its current value.");
        Console.WriteLine();

        try
        {
            var name  = ConsoleHelper.ReadInput($"Name          [{product.Name}]");
            var desc  = ConsoleHelper.ReadInput($"Description   [{product.Description}]");
            var cat   = ConsoleHelper.ReadInput($"Category      [{product.Category}]");
            var price = ConsoleHelper.ReadInput($"Price         [R{product.Price:F2}]");
            var stock = ConsoleHelper.ReadInput($"Stock Qty     [{product.StockQuantity}]");

            if (!string.IsNullOrWhiteSpace(name))                                product.Name          = name;
            if (!string.IsNullOrWhiteSpace(desc))                                product.Description   = desc;
            if (!string.IsNullOrWhiteSpace(cat))                                 product.Category      = cat;
            if (decimal.TryParse(price, out var p) && p > 0)                     product.Price         = p;
            if (int.TryParse(stock, out var s) && s >= 0)                        product.StockQuantity = s;

            _productService.Update(product);
            ConsoleHelper.WriteSuccess($"Product '{product.Name}' updated successfully.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void DeleteProduct()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Delete Product");

        var products = _productService.GetAll();
        if (!products.Any())
        {
            ConsoleHelper.WriteWarning("No products in catalog.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintProductTable(products);
        Console.WriteLine();

        var id = ConsoleHelper.ReadInt("Enter Product ID to delete (0 to cancel)", 0);
        if (id == 0) return;

        var product = _productService.GetById(id);
        if (product == null)
        {
            ConsoleHelper.WriteError("Product not found.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteWarning($"You are about to remove '{product.Name}' from the catalog.");
        var confirm = ConsoleHelper.ReadInput("Type YES to confirm");

        if (!confirm.Equals("YES", StringComparison.Ordinal))
        {
            ConsoleHelper.WriteInfo("Deletion cancelled.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            _productService.Delete(id);
            ConsoleHelper.WriteSuccess($"'{product.Name}' has been removed.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    /// <summary>
    /// Shows all products (highlighting low-stock ones) so the admin can see
    /// which items need restocking before entering an ID.
    /// </summary>
    private void RestockProduct()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Restock Product");

        var products = _productService.GetAll();
        if (!products.Any())
        {
            ConsoleHelper.WriteWarning("No products in catalog.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        // Show low-stock products first for quick reference
        var lowStock = products.Where(p => p.IsLowStock).ToList();
        if (lowStock.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ⚠  Products needing attention (stock ≤ 5):");
            Console.ResetColor();
            PrintProductTable(lowStock);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  All products:");
            Console.ResetColor();
        }

        PrintProductTable(products);
        Console.WriteLine();

        var id = ConsoleHelper.ReadInt("Enter Product ID to restock (0 to cancel)", 0);
        if (id == 0) return;

        var product = _productService.GetById(id);
        if (product == null)
        {
            ConsoleHelper.WriteError("Product not found.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        ConsoleHelper.WriteInfo($"  '{product.Name}'  ·  Current stock: {product.StockQuantity}");
        Console.WriteLine();

        try
        {
            var qty = ConsoleHelper.ReadInt("Units to add", 1);
            _productService.Restock(id, qty);
            ConsoleHelper.WriteSuccess($"Restocked '{product.Name}'. New stock: {product.StockQuantity}.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void ViewAllProducts()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("All Products");

        var products = _productService.GetAll();

        if (!products.Any())
        {
            ConsoleHelper.WriteWarning("No products in catalog.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintProductTable(products);
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  {products.Count} product(s) total  ·  {products.Count(p => p.IsLowStock)} low stock  ·  {products.Count(p => !p.IsInStock)} out of stock");

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Order Management ──────────────────────────────────────────────────────

    private void ViewAllOrders()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("All Orders");

        var orders = _orderService.GetAllOrders();

        if (!orders.Any())
        {
            ConsoleHelper.WriteWarning("No orders have been placed yet.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintOrderTable(orders);
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  {orders.Count} order(s) total");

        ConsoleHelper.PressEnterToContinue();
    }

    /// <summary>
    /// Displays all orders so the admin can see valid IDs and current statuses,
    /// then prompts for the order to update.
    /// </summary>
    private void UpdateOrderStatus()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Update Order Status");

        var orders = _orderService.GetAllOrders();

        if (!orders.Any())
        {
            ConsoleHelper.WriteWarning("No orders found.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintOrderTable(orders);
        Console.WriteLine();

        var orderId = ConsoleHelper.ReadInt("Enter Order ID to update (0 to cancel)", 0);
        if (orderId == 0) return;

        var order = _orderService.GetById(orderId);
        if (order == null)
        {
            ConsoleHelper.WriteError($"Order #{orderId} not found.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  Order #{order.Id}  ·  {order.CustomerName}  ·  Current: {order.Status}");
        Console.WriteLine();

        var statuses = Enum.GetValues<OrderStatus>();

        Console.WriteLine("  New Status:");
        for (int i = 0; i < statuses.Length; i++)
            ConsoleHelper.WriteMenuOption(i + 1, statuses[i].ToString());

        Console.WriteLine();
        var choice    = ConsoleHelper.ReadInt("Select new status", 1, statuses.Length);
        var newStatus = statuses[choice - 1];

        try
        {
            _orderService.UpdateStatus(orderId, newStatus);
            ConsoleHelper.WriteSuccess($"Order #{orderId} status updated to '{newStatus}'.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Private render helpers ────────────────────────────────────────────────

    private static void PrintProductTable(List<Product> products)
    {
        Console.WriteLine($"\n  {"ID",4}  {"Name",-28}  {"Category",-13}  {"Price",10}  {"Stock",7}  Rating");
        Console.WriteLine($"  {new string('─', 76)}");

        foreach (var p in products)
        {
            Console.Write($"  {p.Id,4}  {p.Name,-28}  {p.Category,-13}  R{p.Price,8:F2}  ");

            if (p.StockQuantity == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{"OUT",7}  ");
            }
            else if (p.IsLowStock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{p.StockQuantity,7}  ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{p.StockQuantity,7}  ");
            }

            Console.ResetColor();
            var rating = p.Reviews.Count > 0 ? $"★{p.AverageRating:F1}" : "-";
            Console.WriteLine(rating);
        }
    }

    private static void PrintOrderTable(List<Order> orders)
    {
        Console.WriteLine($"\n  {"ID",4}  {"Customer",-20}  {"Date",-18}  {"Total",10}  Status");
        Console.WriteLine($"  {new string('─', 70)}");

        foreach (var o in orders)
        {
            Console.Write($"  {o.Id,4}  {o.CustomerName,-20}  {o.PlacedAt:dd MMM yyyy HH:mm}  R{o.TotalAmount,8:F2}  ");

            Console.ForegroundColor = o.Status switch
            {
                OrderStatus.Delivered  => ConsoleColor.Green,
                OrderStatus.Shipped    => ConsoleColor.Cyan,
                OrderStatus.Processing => ConsoleColor.Yellow,
                OrderStatus.Cancelled  => ConsoleColor.Red,
                _                      => ConsoleColor.Gray
            };
            Console.WriteLine(o.Status);
            Console.ResetColor();
        }
    }

    // ── Admin Approval ─────────────────────────────────────────────────────────

    /// <summary>
    /// Lists all pending admin registrations and lets the current admin
    /// approve them one by one.
    /// </summary>
    private void ApproveAdmins()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Approve Admin Registrations");

        var pending = _authService.GetPendingAdmins();

        if (!pending.Any())
        {
            ConsoleHelper.WriteInfo("There are no pending admin registrations at this time.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {"ID",4}  {"Username",-16}  {"Full Name",-22}  {"Department",-16}  Registered");
        ConsoleHelper.WriteDivider();

        foreach (var a in pending)
            Console.WriteLine($"  {a.Id,4}  {a.Username,-16}  {a.FullName,-22}  {a.Department,-16}  {a.RegisteredAt:dd MMM yyyy}");

        Console.WriteLine();

        var id = ConsoleHelper.ReadInt("Enter Admin ID to approve (0 to go back)", 0);
        if (id == 0) return;

        var target = pending.FirstOrDefault(a => a.Id == id);
        if (target == null)
        {
            ConsoleHelper.WriteError("That ID was not in the pending list above.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  Approving: {target.FullName} ({target.Username})  —  {target.Department}");
        var confirm = ConsoleHelper.ReadInput("Confirm approval? (yes/no)");

        if (!confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.WriteInfo("No changes made.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            _authService.ApproveAdmin(target.Id);
            ConsoleHelper.WriteSuccess($"'{target.FullName}' has been approved and can now log in.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }
}

using OnlineShoppingSystem.Application.Assistant;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Drives all customer-facing interactions: browsing, cart management,
/// checkout, order tracking, wallet, and reviews.
/// </summary>
public class CustomerMenu
{
    private readonly ProductService  _productService;
    private readonly CartService     _cartService;
    private readonly OrderService    _orderService;
    private readonly PaymentService  _paymentService;
    private readonly ReviewService   _reviewService;
    private readonly AuthService     _authService;
    private readonly WishlistService _wishlistService;
    private readonly ShoppingAssistant _assistant;

    public CustomerMenu(
        ProductService   productService,
        CartService      cartService,
        OrderService     orderService,
        PaymentService   paymentService,
        ReviewService    reviewService,
        AuthService      authService,
        WishlistService  wishlistService,
        ShoppingAssistant assistant)
    {
        _productService  = productService;
        _cartService     = cartService;
        _orderService    = orderService;
        _paymentService  = paymentService;
        _reviewService   = reviewService;
        _authService     = authService;
        _wishlistService = wishlistService;
        _assistant       = assistant;
    }

    public void Run(Customer customer)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader($"Customer Menu  ·  {customer.FullName}");
            ConsoleHelper.WriteStatusBar(
                ("Wallet", $"R{customer.WalletBalance:F2}"),
                ("Cart",   $"{customer.Cart.ItemCount} item(s)  (R{customer.Cart.Total:F2})")
            );
            Console.WriteLine();

            ConsoleHelper.WriteMenuOption(1,  "Browse Products",  "View full catalog by category");
            ConsoleHelper.WriteMenuOption(2,  "Search Products",  "Find products by name or keyword");
            ConsoleHelper.WriteMenuOption(3,  "View Cart",        "See all items in your cart");
            ConsoleHelper.WriteMenuOption(4,  "Update Cart",      "Change quantities or remove items");
            ConsoleHelper.WriteMenuOption(5,  "Checkout",         "Place your order");
            ConsoleHelper.WriteMenuOption(6,  "Wallet",           "View balance and transaction history");
            ConsoleHelper.WriteMenuOption(7,  "Top Up Wallet",    "Add funds to your wallet");
            ConsoleHelper.WriteMenuOption(8,  "Order History",    "View all past orders");
            ConsoleHelper.WriteMenuOption(9,  "Track Order",      "Check the status of an order");
            ConsoleHelper.WriteMenuOption(10, "Write a Review",    "Review a delivered product");
            ConsoleHelper.WriteMenuOption(11, "My Account",        "Edit name or change your password");
            ConsoleHelper.WriteMenuOption(12, "Cancel / Return",   "Cancel or return an order");
            ConsoleHelper.WriteMenuOption(13, "Wishlist",           "Save products for later");
            ConsoleHelper.WriteMenuOption(14, "AI Assistant",       "Ask anything — get smart product suggestions");
            ConsoleHelper.WriteMenuOption(0,  "Logout");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 14);

            switch (choice)
            {
                case 1:  BrowseProducts(customer);      break;
                case 2:  SearchProducts(customer);      break;
                case 3:  ViewCart(customer);            break;
                case 4:  UpdateCart(customer);          break;
                case 5:  Checkout(customer);            break;
                case 6:  ViewWallet(customer);          break;
                case 7:  AddWalletFunds(customer);      break;
                case 8:  ViewOrderHistory(customer);    break;
                case 9:  TrackOrder(customer);          break;
                case 10: ReviewProduct(customer);       break;
                case 11: MyAccount(customer);           break;
                case 12: CancelOrReturnOrder(customer); break;
                case 13: ManageWishlist(customer);      break;
                case 14: AiAssistant(customer);         break;
                case 0:
                    ConsoleHelper.WriteInfo("You have been logged out.");
                    ConsoleHelper.PressEnterToContinue();
                    return;
            }
        }
    }

    // ── Browse & Search ───────────────────────────────────────────────────────

    /// <summary>
    /// Shows the full catalog grouped by category.
    /// The customer may add any in-stock product to their cart without leaving.
    /// </summary>
    private void BrowseProducts(Customer customer)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Product Catalog");

            var products = _productService.GetAll();

            if (!products.Any())
            {
                ConsoleHelper.WriteWarning("No products are currently available.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            PrintProductCatalog(products);
            Console.WriteLine();
            ConsoleHelper.WriteDivider();
            ConsoleHelper.WriteMenuOption(1, "Add a product to cart");
            ConsoleHelper.WriteMenuOption(0, "Back to menu");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 1);
            if (choice == 0) return;

            PromptAddToCart(customer, products);
        }
    }

    /// <summary>
    /// Searches the catalog and lets the customer add results to their cart inline.
    /// </summary>
    private void SearchProducts(Customer customer)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("Search Products");

            var query = ConsoleHelper.ReadRequiredInput("Search term (or 0 to go back)");
            if (query == "0") return;

            var results = _productService.Search(query);

            if (!results.Any())
            {
                ConsoleHelper.WriteWarning($"No products found matching \"{query}\".");
                ConsoleHelper.PressEnterToContinue();
                continue;
            }

            Console.WriteLine();
            ConsoleHelper.WriteInfo($"  {results.Count} result(s) for \"{query}\":");
            Console.WriteLine();
            PrintProductTable(results);

            Console.WriteLine();
            ConsoleHelper.WriteDivider();
            ConsoleHelper.WriteMenuOption(1, "Add a product to cart");
            ConsoleHelper.WriteMenuOption(2, "Search again");
            ConsoleHelper.WriteMenuOption(0, "Back to menu");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 2);
            if (choice == 0) return;
            if (choice == 1) PromptAddToCart(customer, results);
            // choice 2 loops back to search again
        }
    }

    /// <summary>
    /// Asks the customer to pick a product from the given list and add it to their cart.
    /// </summary>
    private void PromptAddToCart(Customer customer, List<Product> availableProducts)
    {
        Console.WriteLine();
        var productId = ConsoleHelper.ReadInt("Enter Product ID to add (0 to cancel)", 0);
        if (productId == 0) return;

        var product = availableProducts.FirstOrDefault(p => p.Id == productId);

        if (product == null)
        {
            ConsoleHelper.WriteError("That product ID was not found in the list above.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        if (!product.IsInStock)
        {
            ConsoleHelper.WriteError($"'{product.Name}' is currently out of stock.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        var alreadyInCart = customer.Cart.Items.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;
        var maxAllowed    = product.StockQuantity - alreadyInCart;

        if (maxAllowed <= 0)
        {
            ConsoleHelper.WriteWarning("You already have the maximum available stock in your cart.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        ConsoleHelper.WriteInfo($"  {product.Name}  ·  R{product.Price:F2}  ·  Up to {maxAllowed} available");
        var qty = ConsoleHelper.ReadInt($"Quantity (1–{maxAllowed})", 1, maxAllowed);

        try
        {
            _cartService.AddToCart(customer, productId, qty);
            ConsoleHelper.WriteSuccess($"{qty}x '{product.Name}' added to cart!  Cart total: R{customer.Cart.Total:F2}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Cart ──────────────────────────────────────────────────────────────────

    private void ViewCart(Customer customer)
    {
        Console.Clear();
        _cartService.DisplayCart(customer);
        ConsoleHelper.PressEnterToContinue();
    }

    private void UpdateCart(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Update Cart");
        _cartService.DisplayCart(customer);

        if (customer.Cart.IsEmpty)
        {
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteMenuOption(1, "Change item quantity");
        ConsoleHelper.WriteMenuOption(2, "Remove an item");
        ConsoleHelper.WriteMenuOption(3, "Clear entire cart");
        ConsoleHelper.WriteMenuOption(0, "Back");
        Console.WriteLine();

        var choice = ConsoleHelper.ReadInt("Select option", 0, 3);
        if (choice == 0) return;

        try
        {
            switch (choice)
            {
                case 1:
                    PrintCartItemIds(customer);
                    var id1    = ConsoleHelper.ReadInt("Product ID to update", 1);
                    var newQty = ConsoleHelper.ReadInt("New quantity (0 to remove)", 0);
                    _cartService.UpdateQuantity(customer, id1, newQty);
                    ConsoleHelper.WriteSuccess("Cart updated.");
                    break;

                case 2:
                    PrintCartItemIds(customer);
                    var id2 = ConsoleHelper.ReadInt("Product ID to remove", 1);
                    _cartService.RemoveFromCart(customer, id2);
                    ConsoleHelper.WriteSuccess("Item removed from cart.");
                    break;

                case 3:
                    var confirm = ConsoleHelper.ReadInput("Clear all items? (yes/no)");
                    if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        _cartService.ClearCart(customer);
                        ConsoleHelper.WriteSuccess("Cart cleared.");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Checkout ──────────────────────────────────────────────────────────────

    private void Checkout(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Checkout");
        _cartService.DisplayCart(customer);

        if (customer.Cart.IsEmpty)
        {
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteDivider();
        Console.WriteLine($"  Order Total    :  R{customer.Cart.Total:F2}");
        Console.WriteLine($"  Wallet Balance :  R{customer.WalletBalance:F2}");

        var shortfall = customer.Cart.Total - customer.WalletBalance;
        if (shortfall > 0)
        {
            Console.WriteLine();
            ConsoleHelper.WriteWarning($"Insufficient funds. Please top up R{shortfall:F2} to proceed.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine($"  Balance After  :  R{customer.WalletBalance - customer.Cart.Total:F2}");
        Console.WriteLine();

        var confirm = ConsoleHelper.ReadInput("Confirm and place order? (yes/no)");

        if (!confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.WriteInfo("Checkout cancelled — your cart has been kept.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            var order = _orderService.PlaceOrder(customer);
            Console.WriteLine();
            ConsoleHelper.WriteSuccess($"Order #{order.Id} placed successfully!");
            ConsoleHelper.WriteInfo($"New wallet balance: R{customer.WalletBalance:F2}");
            ConsoleHelper.WriteInfo($"Use 'Track Order' and enter ID {order.Id} to follow your delivery.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Wallet ────────────────────────────────────────────────────────────────

    private void ViewWallet(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("My Wallet");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  Current Balance:  R{customer.WalletBalance:F2}");
        Console.ResetColor();

        var payments = _paymentService.GetPaymentHistory(customer.Id);

        if (!payments.Any())
        {
            Console.WriteLine();
            ConsoleHelper.WriteInfo("No transactions recorded yet.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {"Date",-22} {"Order",8} {"Amount",12} {"Status",10}");
        ConsoleHelper.WriteDivider();

        foreach (var p in payments.Take(15))
        {
            Console.Write($"  {p.ProcessedAt:dd MMM yyyy HH:mm}  Order #{p.OrderId,-4}  R{p.Amount,9:F2}  ");
            Console.ForegroundColor = p.Status == PaymentStatus.Success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(p.Status);
            Console.ResetColor();
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void AddWalletFunds(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Top Up Wallet");
        ConsoleHelper.WriteInfo($"Current Balance: R{customer.WalletBalance:F2}");
        Console.WriteLine();

        try
        {
            var amount = ConsoleHelper.ReadDecimal("Amount to add (R)", 1);
            _paymentService.AddFunds(customer, amount);
            ConsoleHelper.WriteSuccess($"R{amount:F2} added successfully.");
            ConsoleHelper.WriteInfo($"New balance: R{customer.WalletBalance:F2}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Orders ────────────────────────────────────────────────────────────────

    private void ViewOrderHistory(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Order History");

        var orders = _orderService.GetOrdersByCustomer(customer.Id);

        if (!orders.Any())
        {
            ConsoleHelper.WriteWarning("You haven't placed any orders yet.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        foreach (var order in orders)
        {
            Console.WriteLine();
            PrintOrderSummary(order);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    /// <summary>
    /// Lists all orders first so the customer can see their IDs,
    /// then prompts them to pick one for detailed tracking.
    /// </summary>
    private void TrackOrder(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Track Order");

        var orders = _orderService.GetOrdersByCustomer(customer.Id);

        if (!orders.Any())
        {
            ConsoleHelper.WriteWarning("You have no orders to track.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {"ID",4}  {"Date",-18}  {"Total",10}  Status");
        ConsoleHelper.WriteDivider();

        foreach (var o in orders)
        {
            Console.Write($"  {o.Id,4}  {o.PlacedAt:dd MMM yyyy HH:mm}  R{o.TotalAmount,8:F2}  ");
            PrintStatusColored(o.Status);
            Console.WriteLine();
        }

        Console.WriteLine();
        var orderId = ConsoleHelper.ReadInt("Enter Order ID for details (0 to go back)", 0);
        if (orderId == 0) return;

        var order = orders.FirstOrDefault(o => o.Id == orderId);

        if (order == null)
        {
            ConsoleHelper.WriteError("Order not found in your history.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.Clear();
        ConsoleHelper.WriteHeader($"Order #{order.Id} — Details");
        Console.WriteLine();
        PrintOrderSummary(order, detailed: true);

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Reviews ───────────────────────────────────────────────────────────────

    private void ReviewProduct(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Write a Review");

        // Only products from *delivered* orders are eligible
        var eligibleProducts = customer.OrderHistory
            .Where(o => o.Status == OrderStatus.Delivered)
            .SelectMany(o => o.Items)
            .Select(i => new { i.ProductId, i.ProductName })
            .DistinctBy(i => i.ProductId)
            .ToList();

        if (!eligibleProducts.Any())
        {
            ConsoleHelper.WriteWarning("You have no eligible products to review.");
            ConsoleHelper.WriteInfo("Reviews are only available once your order has been delivered.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {"ID",4}  Product");
        ConsoleHelper.WriteDivider();

        foreach (var p in eligibleProducts)
            Console.WriteLine($"  {p.ProductId,4}  {p.ProductName}");

        Console.WriteLine();

        try
        {
            var productId = ConsoleHelper.ReadInt("Product ID to review (0 to cancel)", 0);
            if (productId == 0) return;

            Console.WriteLine();
            Console.WriteLine("  Rating guide:  1=Poor  2=Fair  3=Good  4=Great  5=Excellent");
            var rating  = ConsoleHelper.ReadInt("Your rating (1–5)", 1, 5);
            var comment = ConsoleHelper.ReadRequiredInput("Your comment");

            _reviewService.SubmitReview(customer, productId, rating, comment);
            ConsoleHelper.WriteSuccess("Review submitted — thank you for your feedback!");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Cancel / Return ───────────────────────────────────────────────────────

    /// <summary>
    /// Shows cancellable and returnable orders, lets the customer pick one,
    /// then routes to the correct action based on its status.
    /// </summary>
    private void CancelOrReturnOrder(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Cancel / Return Order");

        var actionableOrders = customer.OrderHistory
            .Where(o => o.Status == OrderStatus.Pending   ||
                        o.Status == OrderStatus.Processing ||
                        o.Status == OrderStatus.Delivered)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

        if (!actionableOrders.Any())
        {
            ConsoleHelper.WriteWarning("You have no orders available to cancel or return.");
            ConsoleHelper.WriteInfo("  • Cancel: available for Pending or Processing orders.");
            ConsoleHelper.WriteInfo("  • Return:  available for Delivered orders.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {"ID",4}  {"Date",-18}  {"Total",10}  {"Status",-12}  Action");
        ConsoleHelper.WriteDivider();

        foreach (var o in actionableOrders)
        {
            var action = o.Status == OrderStatus.Delivered ? "Return" : "Cancel";
            Console.Write($"  {o.Id,4}  {o.PlacedAt:dd MMM yyyy HH:mm}  R{o.TotalAmount,8:F2}  ");
            PrintStatusColored(o.Status);
            Console.WriteLine($"  {action}");
        }

        Console.WriteLine();
        var orderId = ConsoleHelper.ReadInt("Enter Order ID (0 to go back)", 0);
        if (orderId == 0) return;

        var order = actionableOrders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
        {
            ConsoleHelper.WriteError("That order ID was not in the list above.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        if (order.Status == OrderStatus.Delivered)
            ConfirmReturnOrder(customer, order);
        else
            ConfirmCancelOrder(customer, order);
    }

    private void ConfirmCancelOrder(Customer customer, Order order)
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  Order #{order.Id}  ·  R{order.TotalAmount:F2}  ·  Status: {order.Status}");
        ConsoleHelper.WriteInfo($"  A full refund of R{order.TotalAmount:F2} will be returned to your wallet.");
        Console.WriteLine();

        var confirm = ConsoleHelper.ReadInput("Cancel this order? (yes/no)");
        if (!confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.WriteInfo("No changes made.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            _orderService.CancelOrder(customer, order.Id);
            ConsoleHelper.WriteSuccess($"Order #{order.Id} cancelled. R{order.TotalAmount:F2} refunded to your wallet.");
            ConsoleHelper.WriteInfo($"New wallet balance: R{customer.WalletBalance:F2}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void ConfirmReturnOrder(Customer customer, Order order)
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  Order #{order.Id}  ·  R{order.TotalAmount:F2}  ·  Status: Delivered");
        ConsoleHelper.WriteInfo($"  Returning this order will refund R{order.TotalAmount:F2} to your wallet.");
        Console.WriteLine();

        var confirm = ConsoleHelper.ReadInput("Return this order? (yes/no)");
        if (!confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.WriteInfo("No changes made.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            _orderService.ReturnOrder(customer, order.Id);
            ConsoleHelper.WriteSuccess($"Order #{order.Id} returned. R{order.TotalAmount:F2} refunded to your wallet.");
            ConsoleHelper.WriteInfo($"New wallet balance: R{customer.WalletBalance:F2}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Wishlist ──────────────────────────────────────────────────────────────

    private void ManageWishlist(Customer customer)
    {
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader("My Wishlist");

            var wishlist = _wishlistService.GetWishlist(customer);

            if (!wishlist.Any())
            {
                ConsoleHelper.WriteWarning("Your wishlist is empty.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"  {"ID",4}  {"Name",-28}  {"Price",10}  {"Stock",7}");
                ConsoleHelper.WriteDivider();

                foreach (var p in wishlist)
                {
                    Console.Write($"  {p.Id,4}  {p.Name,-28}  R{p.Price,8:F2}  ");
                    Console.ForegroundColor = p.IsInStock ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine(p.IsInStock ? $"{p.StockQuantity,7}" : "    OUT");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            ConsoleHelper.WriteMenuOption(1, "Add product to wishlist");
            ConsoleHelper.WriteMenuOption(2, "Remove product from wishlist");
            ConsoleHelper.WriteMenuOption(3, "Add wishlist item to cart");
            ConsoleHelper.WriteMenuOption(0, "Back");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 3);
            if (choice == 0) return;

            switch (choice)
            {
                case 1: AddToWishlist(customer);              break;
                case 2: RemoveFromWishlist(customer, wishlist); break;
                case 3: AddWishlistItemToCart(customer, wishlist); break;
            }
        }
    }

    private void AddToWishlist(Customer customer)
    {
        Console.WriteLine();

        // Show all products so the user can see IDs before entering one
        var allProducts = _productService.GetAll();

        if (!allProducts.Any())
        {
            ConsoleHelper.WriteWarning("No products are currently available.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        PrintProductTable(allProducts);
        Console.WriteLine();

        var productId = ConsoleHelper.ReadInt("Enter Product ID to add to wishlist (0 to cancel)", 0);
        if (productId == 0) return;

        try
        {
            _wishlistService.AddToWishlist(customer, productId);
            ConsoleHelper.WriteSuccess("Product added to your wishlist.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void RemoveFromWishlist(Customer customer, List<Product> wishlist)
    {
        if (!wishlist.Any())
        {
            ConsoleHelper.WriteWarning("Your wishlist is empty.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        var productId = ConsoleHelper.ReadInt("Enter Product ID to remove (0 to cancel)", 0);
        if (productId == 0) return;

        try
        {
            _wishlistService.RemoveFromWishlist(customer, productId);
            ConsoleHelper.WriteSuccess("Product removed from your wishlist.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void AddWishlistItemToCart(Customer customer, List<Product> wishlist)
    {
        var inStock = wishlist.Where(p => p.IsInStock).ToList();

        if (!inStock.Any())
        {
            ConsoleHelper.WriteWarning("None of your wishlist items are currently in stock.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        var productId = ConsoleHelper.ReadInt("Enter Product ID to add to cart (0 to cancel)", 0);
        if (productId == 0) return;

        var product = inStock.FirstOrDefault(p => p.Id == productId);
        if (product == null)
        {
            ConsoleHelper.WriteError("That product is not in your wishlist or is out of stock.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        var alreadyInCart = customer.Cart.Items.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;
        var maxAllowed    = product.StockQuantity - alreadyInCart;

        ConsoleHelper.WriteInfo($"  {product.Name}  ·  R{product.Price:F2}  ·  Up to {maxAllowed} available");
        var qty = ConsoleHelper.ReadInt($"Quantity (1–{maxAllowed})", 1, maxAllowed);

        try
        {
            _cartService.AddToCart(customer, productId, qty);
            ConsoleHelper.WriteSuccess($"{qty}x '{product.Name}' added to cart!");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── My Account ────────────────────────────────────────────────────────────

    /// <summary>Account settings sub-menu: edit name or change password.</summary>
    private void MyAccount(Customer customer)
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("My Account");
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"  Username  : {customer.Username}");
        ConsoleHelper.WriteInfo($"  Full Name : {customer.FullName}");
        ConsoleHelper.WriteInfo($"  Email     : {customer.Email}");
        Console.WriteLine();

        ConsoleHelper.WriteMenuOption(1, "Edit Full Name",    "Update your display name");
        ConsoleHelper.WriteMenuOption(2, "Change Password",   "Update your password securely");
        ConsoleHelper.WriteMenuOption(0, "Back");
        Console.WriteLine();

        var choice = ConsoleHelper.ReadInt("Select option", 0, 2);

        switch (choice)
        {
            case 1: EditFullName(customer);    break;
            case 2: ChangePassword(customer);  break;
        }
    }

    private void EditFullName(Customer customer)
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo($"Current name: {customer.FullName}");
        var newName = ConsoleHelper.ReadRequiredInput("New full name");

        try
        {
            _authService.UpdateFullName(customer, newName);
            ConsoleHelper.WriteSuccess($"Name updated to '{customer.FullName}'.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void ChangePassword(Customer customer)
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo("Password requirements: 6+ characters, uppercase, lowercase, number, symbol.");
        Console.WriteLine();

        var current = ConsoleHelper.ReadPassword("Current password");

        // Verify current password before revealing the new-password prompt
        if (!Application.Helpers.PasswordHelper.Verify(current, customer.PasswordHash))
        {
            ConsoleHelper.WriteError("Current password is incorrect.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        // Loop until a strong new password is entered
        string newPassword;
        while (true)
        {
            newPassword = ConsoleHelper.ReadPassword("New password");
            var error = Application.Helpers.PasswordHelper.GetStrengthError(newPassword);
            if (error == null) break;
            ConsoleHelper.WriteWarning(error);
        }

        var confirm = ConsoleHelper.ReadPassword("Confirm new password");

        if (newPassword != confirm)
        {
            ConsoleHelper.WriteError("Passwords do not match. No changes were saved.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        try
        {
            _authService.ChangePassword(customer, current, newPassword);
            ConsoleHelper.WriteSuccess("Password changed successfully.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Private render helpers ────────────────────────────────────────────────

    private static void PrintProductCatalog(List<Product> products)
    {
        var grouped = products.GroupBy(p => p.Category).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  ▸ {group.Key.ToUpper()}");
            Console.ResetColor();
            Console.WriteLine($"  {"ID",4}  {"Name",-28}  {"Price",10}  {"Stock",7}  Rating");
            Console.WriteLine($"  {new string('─', 60)}");

            foreach (var p in group)
                PrintProductRow(p);
        }
    }

    private static void PrintProductTable(List<Product> products)
    {
        Console.WriteLine($"  {"ID",4}  {"Name",-28}  {"Category",-13}  {"Price",10}  {"Stock",7}  Rating");
        Console.WriteLine($"  {new string('─', 76)}");

        foreach (var p in products)
            PrintProductRow(p, showCategory: true);
    }

    private static void PrintProductRow(Product p, bool showCategory = false)
    {
        Console.Write($"  {p.Id,4}  {p.Name,-28}  ");

        if (showCategory)
            Console.Write($"{p.Category,-13}  ");

        Console.Write($"R{p.Price,8:F2}  ");

        if (!p.IsInStock)
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

        var stars = p.Reviews.Count > 0 ? $"★{p.AverageRating:F1}" : "No reviews";
        Console.WriteLine(stars);
    }

    private static void PrintOrderSummary(Order order, bool detailed = false)
    {
        Console.Write($"  Order #{order.Id,-5}  {order.PlacedAt:dd MMM yyyy HH:mm}  R{order.TotalAmount,9:F2}  Status: ");
        PrintStatusColored(order.Status);
        Console.WriteLine();

        if (!detailed) return;

        Console.WriteLine();
        Console.WriteLine($"  {"Product",-30}  {"Qty",5}  {"Unit Price",11}  {"Total",10}");
        Console.WriteLine($"  {new string('─', 62)}");

        foreach (var item in order.Items)
            Console.WriteLine($"  {item.ProductName,-30}  {item.Quantity,5}  R{item.UnitPrice,9:F2}  R{item.LineTotal,8:F2}");

        if (order.LastUpdated.HasValue)
        {
            Console.WriteLine();
            ConsoleHelper.WriteInfo($"Last updated: {order.LastUpdated:dd MMM yyyy HH:mm}");
        }
    }

    private static void PrintStatusColored(OrderStatus status)
    {
        Console.ForegroundColor = status switch
        {
            OrderStatus.Delivered  => ConsoleColor.Green,
            OrderStatus.Shipped    => ConsoleColor.Cyan,
            OrderStatus.Processing => ConsoleColor.Yellow,
            OrderStatus.Cancelled  => ConsoleColor.Red,
            _                      => ConsoleColor.Gray
        };
        Console.Write($"{status,-12}");
        Console.ResetColor();
    }

    private static void PrintCartItemIds(Customer customer)
    {
        Console.WriteLine();
        foreach (var item in customer.Cart.Items)
            ConsoleHelper.WriteInfo($"  [{item.ProductId}] {item.ProductName}  (qty: {item.Quantity})");
        Console.WriteLine();
    }

    // ── AI Shopping Assistant ──────────────────────────────────────────────────

    /// <summary>
    /// Interactive chat loop for the rule-based AI shopping assistant.
    /// Runs until the user types 'back' or submits an empty line.
    /// </summary>
    private void AiAssistant(Customer customer)
    {
        Console.Clear();
        AssistantRenderer.RenderChatHeader(customer.FullName, customer.WalletBalance);

        while (true)
        {
            Console.Write("  You: ");
            var input = Console.ReadLine()?.Trim() ?? string.Empty;

            // Exit conditions
            if (string.IsNullOrWhiteSpace(input) ||
                input.Equals("back",   StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit",   StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit",   StringComparison.OrdinalIgnoreCase))
            {
                _assistant.ResetContext();
                Console.WriteLine();
                ConsoleHelper.WriteInfo("Leaving the assistant. Back to your menu.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            AssistantRenderer.RenderUserMessage(input);

            var response = _assistant.Ask(input);
            AssistantRenderer.Render(response);

            Console.WriteLine();
            Console.WriteLine(new string('─', 60));
        }
    }
}

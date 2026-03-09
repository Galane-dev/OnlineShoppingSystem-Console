using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Application entry point — wires up all dependencies manually (no DI container)
/// and launches the main menu loop.
/// </summary>

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Compose the application ────────────────────────────────────────────────
var store           = new DataStore();
var authService     = new AuthService(store);
var productService  = new ProductService(store);
var paymentService  = new PaymentService(store);
var orderService    = new OrderService(store, paymentService);
var cartService     = new CartService(store);
var reviewService   = new ReviewService(store);
var reportService   = new ReportService(store);
var wishlistService = new WishlistService(store);

var customerMenu = new CustomerMenu(productService, cartService, orderService, paymentService, reviewService, authService, wishlistService);
var adminMenu    = new AdminMenu(productService, orderService, reportService);
var mainMenu     = new MainMenu(authService, customerMenu, adminMenu);

// ── Run ────────────────────────────────────────────────────────────────────
mainMenu.Run();

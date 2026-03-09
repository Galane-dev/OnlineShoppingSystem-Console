using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Repositories;
using OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Application entry point.
/// Wires up all dependencies manually (Pure DI / Poor Man's DI):
///   DataStore → Repositories → Services → Menus
/// </summary>

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Data layer ─────────────────────────────────────────────────────────────
var store = new DataStore();

// ── Repositories (Repository Pattern) ─────────────────────────────────────
var userRepository    = new UserRepository(store);
var productRepository = new ProductRepository(store);
var orderRepository   = new OrderRepository(store);
var paymentRepository = new PaymentRepository(store);

// ── Services ───────────────────────────────────────────────────────────────
var authService     = new AuthService(userRepository, store);
var productService  = new ProductService(productRepository, store);
var paymentService  = new PaymentService(paymentRepository, store);
var orderService    = new OrderService(orderRepository, productRepository, paymentService, store);
var cartService     = new CartService(productRepository);
var reviewService   = new ReviewService(productRepository, store);
var reportService   = new ReportService(orderRepository, productRepository);
var wishlistService = new WishlistService(productRepository);

// ── Menus ──────────────────────────────────────────────────────────────────
var customerMenu = new CustomerMenu(productService, cartService, orderService,
                                    paymentService, reviewService, authService, wishlistService);
var adminMenu    = new AdminMenu(productService, orderService, reportService, authService);
var mainMenu     = new MainMenu(authService, customerMenu, adminMenu);

// ── Run ────────────────────────────────────────────────────────────────────
mainMenu.Run();

# Online Shopping System — C# Console Application

## How to Run

```bash
cd OnlineShoppingSystem
dotnet run
```

Data is saved to a `data/` folder next to the executable. On first run it is seeded automatically. Subsequent runs load your saved state.

## Default Login Credentials

| Role          | Username    | Password  |
|---------------|-------------|-----------|
| Administrator | admin       | admin123  |
| Customer      | john_doe    | pass123   |
| Customer      | jane_smith  | pass123   |

---

## Architecture

```
OnlineShoppingSystem/
│
├── Program.cs                          # Entry point & manual DI wiring
│
├── Domain/
│   ├── Models/
│   │   ├── User.cs                     # Abstract base user
│   │   ├── Customer.cs                 # Inherits User; owns Cart, Orders, Reviews
│   │   ├── Administrator.cs            # Inherits User
│   │   ├── Product.cs                  # Catalog item (stock, reviews, ratings)
│   │   ├── Cart.cs                     # CartItem + Cart aggregate
│   │   ├── Order.cs                    # OrderItem + Order (OrderStatus enum)
│   │   ├── Payment.cs                  # Wallet transaction (PaymentStatus enum)
│   │   └── Review.cs                   # Star rating + comment
│   └── Interfaces/
│       └── IServices.cs                # IProductService, ICartService, IOrderService,
│                                       # IPaymentService, IAuthService, IReportService
│
├── Application/
│   └── Services/
│       ├── AuthService.cs              # Registration & login
│       ├── ProductService.cs           # CRUD + LINQ search/filter
│       ├── CartService.cs              # Cart mutations with stock validation
│       ├── OrderService.cs             # Order placement + stock decrement
│       ├── PaymentService.cs           # Wallet debit & top-up
│       ├── ReviewService.cs            # Review submission with purchase guard
│       └── ReportService.cs            # LINQ-powered analytics
│
├── Infrastructure/
│   └── Data/
│       ├── DataStore.cs                # In-memory store + first-run seed
│       └── JsonPersistence.cs          # JSON load/save with polymorphic User converter
│
├── Presentation/
│   ├── Helpers/
│   │   └── ConsoleHelper.cs            # Colour output, prompts, status bar
│   └── Menus/
│       ├── MainMenu.cs                 # Register / Login / Exit
│       ├── CustomerMenu.cs             # 10 customer actions (inline add-to-cart)
│       └── AdminMenu.cs                # 11 admin actions (list-before-pick UX)
│
└── data/                               # Auto-created at runtime
    ├── users.json
    ├── products.json
    ├── orders.json
    ├── payments.json
    └── sequences.json
```

---

## Key Technical Highlights

| Criterion | Implementation |
|---|---|
| **OOP / Inheritance** | `Customer` and `Administrator` inherit abstract `User`; polymorphic `Role` |
| **Interfaces** | 6 service interfaces in `Domain/Interfaces` decouple contract from implementation |
| **LINQ** | Filtering, groupBy, aggregation, projection throughout all services |
| **Exception Handling** | Guard clauses in every service; all menu actions wrapped in try/catch |
| **JSON Persistence** | `JsonPersistence` reads/writes all collections on every mutation; polymorphic user converter |
| **UX: Add to cart inline** | Browse and Search both offer an "Add to cart" action without leaving the screen |
| **UX: Track order** | Lists all orders with IDs before asking which one to detail |
| **UX: Admin restock/update** | Shows full product table before prompting for ID |
| **UX: Admin order status** | Shows all orders with current status before prompting for ID |
| **Code Quality** | XML doc comments, guard clauses, single responsibility, no magic numbers |

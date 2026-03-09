# Online Shopping System — C# Console Application


A fully-featured backend shopping system built in **.NET 10** as a C# console application. Demonstrates clean architecture, design patterns, LINQ, JSON persistence, and a complete CI pipeline.

---

## Quick Start

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
# Run the application
cd OnlineShoppingSystem
dotnet run

# Run all unit tests
dotnet test OnlineShoppingSystem.sln
```

Data is saved to a `data/` folder next to the executable on first run and loaded automatically on subsequent runs. Delete `data/` to reset to seed data.

---

## Default Login Credentials

| Role          | Username    | Password   | Notes                        |
|---------------|-------------|------------|------------------------------|
| Administrator | `admin`     | `admin123` | Pre-approved, full access    |
| Customer      | `john_doe`  | `pass123`  | R500 wallet balance          |
| Customer      | `jane_smith`| `pass123`  | R1200 wallet balance         |

> **Note:** Seed passwords may not meet the strong-password rules enforced at registration. They exist only for convenient first-run testing. Delete `data/` and re-seed if you need fresh accounts with strong passwords.

---

## Features

### Customer
- Browse products grouped by category
- Fuzzy search by name and description (results ranked by relevance)
- Shopping cart with stock validation
- Checkout via wallet balance
- Order history and order tracking
- Cancel orders (Pending/Processing) or return orders (Delivered) with automatic refund
- Wallet top-up and transaction history
- Wishlist — save products for later, add directly to cart from wishlist
- Write reviews (only after order is delivered; one review per product)
- My Account — edit display name, change password

### Administrator
- Full product CRUD (soft-delete preserves order history)
- Restock inventory
- View and update order statuses
- Sales, top-products, low-stock, and customer-spend reports
- **Approve pending admin registrations** — new admins cannot log in until approved

### Authentication
- Masked password input (asterisks)
- Strong password enforcement (6+ chars, uppercase, lowercase, digit, symbol)
- Security question + hashed answer set at registration
- Password reset via security question
- SHA-256 password hashing with application salt
- Unapproved admin accounts are blocked at login with a clear message

---

## Architecture

```
OnlineShoppingSystem/
│
├── Program.cs                              # Entry point — Pure DI wiring
│
├── Domain/
│   ├── Models/
│   │   ├── User.cs                         # Abstract base: credentials, security Q&A
│   │   ├── Customer.cs                     # Inherits User; Cart, OrderHistory, Wishlist
│   │   ├── Administrator.cs                # Inherits User; Department, IsApproved flag
│   │   ├── Product.cs                      # Catalog item; soft-delete via IsActive
│   │   ├── Cart.cs                         # CartItem + Cart aggregate
│   │   ├── Order.cs                        # OrderItem + Order (OrderStatus enum)
│   │   ├── Payment.cs                      # Wallet transaction (PaymentStatus enum)
│   │   └── Review.cs                       # Star rating + comment
│   └── Interfaces/
│       ├── IServices.cs                    # Service contracts
│       └── IRepositories.cs                # Repository contracts
│
├── Application/
│   ├── Helpers/
│   │   ├── PasswordHelper.cs               # SHA-256 hashing + strength validation
│   │   └── FuzzyMatcher.cs                 # Tiered fuzzy scoring for search
│   ├── Services/
│   │   ├── AuthService.cs                  # Login, registration (customer + admin), approval
│   │   ├── ProductService.cs               # CRUD, fuzzy search, stock queries
│   │   ├── CartService.cs                  # Cart mutations with stock validation
│   │   ├── OrderService.cs                 # Order placement, cancel, return
│   │   ├── PaymentService.cs               # Wallet debit and top-up
│   │   ├── ReviewService.cs                # Review submission with delivery guard
│   │   ├── WishlistService.cs              # Wishlist add/remove/list
│   │   └── ReportService.cs                # LINQ-powered analytics
│   └── Session/
│       └── UserSession.cs                  # Singleton — tracks logged-in user
│
├── Infrastructure/
│   ├── Data/
│   │   ├── DataStore.cs                    # In-memory collections + JSON persistence
│   │   └── JsonPersistence.cs              # Load/save with polymorphic User converter
│   └── Repositories/
│       ├── UserRepository.cs               # IUserRepository implementation
│       └── Repositories.cs                 # ProductRepository, OrderRepository, PaymentRepository
│
├── Presentation/
│   ├── Helpers/
│   │   └── ConsoleHelper.cs                # Colour output, masked input, status bar
│   └── Menus/
│       ├── MainMenu.cs                     # Register (customer/admin), Login, Forgot Password
│       ├── CustomerMenu.cs                 # 13 customer actions
│       └── AdminMenu.cs                    # 12 admin actions including approval
│
└── data/                                   # Auto-created at runtime
    ├── users.json
    ├── products.json
    ├── orders.json
    ├── payments.json
    └── sequences.json
```

---

## Design Patterns & Decisions

### Repository Pattern
Services never access `DataStore` collections directly. Instead they depend on `IUserRepository`, `IProductRepository`, `IOrderRepository`, and `IPaymentRepository`. This means:
- Business logic is decoupled from storage concerns
- Repositories can be swapped (e.g. SQL database) without touching any service
- Unit tests can inject an in-memory implementation without touching the filesystem

### Singleton Pattern — `UserSession`
`UserSession.Current` is the single global instance tracking who is currently logged in. It is set on successful login and cleared on logout. The pattern is implemented with a static readonly field (thread-safe in .NET) and a private constructor to prevent external instantiation.

### Inheritance & Polymorphism
`Customer` and `Administrator` both inherit the abstract `User` class. The `Role` property is abstract and overridden in each subclass. JSON persistence uses a `$type` discriminator field to round-trip polymorphic users correctly.

### Fuzzy Search
`FuzzyMatcher.Score` returns a value in `[0, 1]` using a four-tier algorithm:
1. **Exact phrase** anywhere in the text → `1.0`
2. **All query words** present → `0.8`
3. **Some query words** present → proportional score up to `0.4`
4. **Character subsequence** overlap → proportional score up to `0.2`

`ProductService.Search` scores both `Name` and `Description`, takes the higher score, filters out zero-score results, and returns products ordered best-match first.

### Soft Delete
Products are never removed from the database. `IsActive = false` hides them from all catalog queries while preserving their identity in historical order data.

### Guard Clauses
Every service method validates inputs at the top and throws descriptive exceptions early, keeping the happy path unindented and readable.

### Admin Approval Workflow
1. A new admin registers via `[2] Register as Admin` on the main menu. Their account is saved with `IsApproved = false`.
2. When they attempt to log in, `AuthService.Login` detects the unapproved state and throws a clear message instead of returning a user.
3. An existing approved admin sees a pending count badge on `[12] Approve Admins` in their menu.
4. After reviewing the registration, the admin types `yes` to approve. The flag is set to `true` and saved. The new admin can now log in normally.

---

## Testing

The `OnlineShoppingSystem.Tests` project uses **xUnit** and **FluentAssertions**.

```
OnlineShoppingSystem.Tests/
├── TestFixture.cs              # Shared setup: temp DataStore, all services wired
├── PasswordHelperTests.cs      # Hashing and strength validation
├── FuzzyMatcherTests.cs        # Scoring tiers and ranking
├── AuthServiceTests.cs         # Registration, login, password reset, approval
├── ProductServiceTests.cs      # CRUD, search, soft-delete, restock
├── CartServiceTests.cs         # Add/remove/update with stock enforcement
├── OrderServiceTests.cs        # PlaceOrder, Cancel, Return, refunds, restock
├── ReviewServiceTests.cs       # Delivered-order guard, duplicates, rating bounds
└── WishlistServiceTests.cs     # Add, remove, ordering, soft-deleted products
```

Each test class creates its own `TestFixture` (which spins up a temp directory) and disposes it in `Dispose()`. Tests are fully isolated with no shared state.

---

## CI — GitHub Actions

`.github/workflows/ci.yml` runs on every push and pull request to `main`/`master`:

1. **Checkout** the repository
2. **Set up .NET 10**
3. **Restore** NuGet packages
4. **Build** in Release configuration
5. **Run all tests** and upload results as a build artifact



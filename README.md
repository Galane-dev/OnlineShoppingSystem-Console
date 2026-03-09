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

> **Note:** Seed passwords do not meet the strong-password rules enforced at registration. They exist only for convenient first-run testing. Delete `data/` and re-seed if you need fresh accounts with strong passwords.

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
- **AI Shopping Assistant** — natural language product suggestions (no internet required)

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
├── Application/
│   ├── Assistant/
│   │   ├── QueryParser.cs                  # Parses natural language → ParsedQuery (intent, budget, categories, keywords)
│   │   ├── ProductScorer.cs                # Scores products against a ParsedQuery using weighted signals
│   │   ├── ResponseComposer.cs             # Builds natural-language AssistantResponse from scored results
│   │   └── ShoppingAssistant.cs            # Orchestrates the pipeline; single public entry point
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

### Rule-Based AI Shopping Assistant

The assistant works entirely offline — no API keys, no internet, no external models. It uses a four-stage pipeline:

```
User: "With R500, what can I buy for the office?"
         │
         ▼
  ┌─────────────────────────────────────────────────────┐
  │ Stage 1 — QueryParser                               │
  │  Intent   : BudgetBased                             │
  │  MaxBudget: R500                                    │
  │  Categories: ["Furniture", "Electronics"]           │
  │  UseCases : ["office"]                              │
  │  Keywords : ["office"]                              │
  └────────────────────┬────────────────────────────────┘
                       │
                       ▼
  ┌─────────────────────────────────────────────────────┐
  │ Stage 2 — ProductScorer                             │
  │  For each in-stock product:                         │
  │    · Hard filter: price > R500 → excluded           │
  │    · Budget fit signal   (weight 3.0)               │
  │    · Name fuzzy match    (weight 2.5)               │
  │    · Desc fuzzy match    (weight 1.5)               │
  │    · Category match      (weight 2.0)               │
  │    · Use-case match      (weight 1.8)               │
  │    · Rating bonus        (weight 0.8)               │
  │    · Value-for-money     (weight 1.2)               │
  │  → ranked list of ScoredProduct with reasons[]      │
  └────────────────────┬────────────────────────────────┘
                       │
                       ▼
  ┌─────────────────────────────────────────────────────┐
  │ Stage 3 — ResponseComposer                          │
  │  Selects intent-specific template                   │
  │  Builds intro, product lines, footer tip            │
  │  → AssistantResponse                                │
  └────────────────────┬────────────────────────────────┘
                       │
                       ▼
  ┌─────────────────────────────────────────────────────┐
  │ Stage 4 — AssistantRenderer (Presentation)          │
  │  Renders coloured output with star ratings,         │
  │  why-chosen reasons, product IDs for cart actions   │
  └─────────────────────────────────────────────────────┘
```

**Supported query types:**

| Example query | Detected intent |
|---|---|
| `"With R500, what can I buy?"` | BudgetBased |
| `"I need a gift for someone who likes fitness"` | GiftSearch |
| `"Compare your laptops"` | Comparison |
| `"What are your best rated products?"` | TopRated |
| `"Show me electronics under R1000"` | CategoryBased |
| `"Something portable for travel"` | Recommendation |

The assistant explains *why* each product was recommended ("fits your budget · in Electronics · suits office use") so the user understands the reasoning, not just the result.

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


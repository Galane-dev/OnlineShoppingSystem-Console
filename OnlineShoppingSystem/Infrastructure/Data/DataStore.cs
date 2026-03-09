using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Infrastructure.Data;

/// <summary>
/// In-memory data store backed by JSON persistence.
/// On startup it loads from disk; if no data exists it seeds defaults.
/// Call Save() after any mutation to persist changes.
/// </summary>
public class DataStore
{
    // ── Sequences ──────────────────────────────────────────────────────────────
    private int _userIdSeq    = 1;
    private int _productIdSeq = 1;
    private int _orderIdSeq   = 1;
    private int _paymentIdSeq = 1;
    private int _reviewIdSeq  = 1;

    public int NextUserId()    => _userIdSeq++;
    public int NextProductId() => _productIdSeq++;
    public int NextOrderId()   => _orderIdSeq++;
    public int NextPaymentId() => _paymentIdSeq++;
    public int NextReviewId()  => _reviewIdSeq++;

    public SequenceData GetSequences() => new()
    {
        UserId    = _userIdSeq,
        ProductId = _productIdSeq,
        OrderId   = _orderIdSeq,
        PaymentId = _paymentIdSeq,
        ReviewId  = _reviewIdSeq
    };

    public void RestoreSequences(SequenceData s)
    {
        _userIdSeq    = s.UserId;
        _productIdSeq = s.ProductId;
        _orderIdSeq   = s.OrderId;
        _paymentIdSeq = s.PaymentId;
        _reviewIdSeq  = s.ReviewId;
    }

    // ── Collections ────────────────────────────────────────────────────────────
    public List<User>    Users    { get; } = new();
    public List<Product> Products { get; } = new();
    public List<Order>   Orders   { get; } = new();
    public List<Payment> Payments { get; } = new();

    private readonly JsonPersistence _persistence;

    public DataStore(string dataDirectory = "data")
    {
        _persistence = new JsonPersistence(dataDirectory);

        if (!_persistence.TryLoadAll(this))
        {
            Seed();
            Save();
        }
    }

    /// <summary>
    /// Constructor for unit tests — creates a completely empty, unseeded store
    /// with no file I/O. Not for production use.
    /// </summary>
    internal DataStore(bool testMode)
    {
        _persistence = null!;   // never called in test mode
    }

    /// <summary>Persists all collections and sequences to disk. No-op in test mode.</summary>
    public void Save()
    {
        if (_persistence != null)
            _persistence.SaveAll(this);
    }

    // ── Seed Data ──────────────────────────────────────────────────────────────

    /// <summary>First-run seed: creates a default admin, two customers, and ten products.</summary>
    private void Seed()
    {
        SeedAdmin();
        SeedCustomers();
        SeedProducts();
    }

    private void SeedAdmin()
    {
        Users.Add(new Administrator
        {
            Id           = NextUserId(),
            Username     = "admin",
            Email        = "admin@shop.com",
            PasswordHash = HashPassword("admin123"),
            FullName     = "System Administrator",
            Department   = "Operations",
            IsApproved   = true   // seeded admins are pre-approved
        });
    }

    private void SeedCustomers()
    {
        var rows = new[]
        {
            ("john_doe",   "john@email.com",  "pass123", "John Doe",   500m),
            ("jane_smith", "jane@email.com",  "pass123", "Jane Smith", 1200m),
        };

        foreach (var (username, email, pass, name, wallet) in rows)
        {
            var c = new Customer
            {
                Id            = NextUserId(),
                Username      = username,
                Email         = email,
                PasswordHash  = HashPassword(pass),
                FullName      = name,
                WalletBalance = wallet
            };
            c.Cart = new Cart { CustomerId = c.Id };
            Users.Add(c);
        }
    }

    private void SeedProducts()
    {
        var rows = new[]
        {
            ("Laptop Pro 15",     "High-performance 15\" laptop",              "Electronics",  15999.99m, 12),
            ("Wireless Mouse",    "Ergonomic wireless mouse",                  "Electronics",    349.99m, 50),
            ("USB-C Hub",         "7-in-1 USB-C hub adapter",                  "Electronics",    599.99m,  8),
            ("Running Shoes",     "Lightweight trail running shoes",            "Footwear",       899.99m, 25),
            ("Yoga Mat",          "Non-slip premium yoga mat",                 "Fitness",        299.99m, 30),
            ("Coffee Maker",      "12-cup programmable coffee maker",          "Appliances",    1299.99m,  4),
            ("Clean Code (Book)", "Software craftsmanship guide by R. Martin", "Books",          499.99m, 15),
            ("LED Desk Lamp",     "Adjustable brightness desk lamp",           "Furniture",      449.99m,  3),
            ("Backpack 30L",      "Durable water-resistant travel backpack",   "Accessories",    699.99m, 20),
            ("Bluetooth Speaker", "Portable waterproof speaker, 20hr battery", "Electronics",   799.99m,  6),
        };

        foreach (var (name, desc, cat, price, stock) in rows)
        {
            Products.Add(new Product
            {
                Id            = NextProductId(),
                Name          = name,
                Description   = desc,
                Category      = cat,
                Price         = price,
                StockQuantity = stock
            });
        }
    }

    // ── Password helpers ───────────────────────────────────────────────────────

    /// <summary>Delegates to PasswordHelper for SHA-256 hashing.</summary>
    public static string HashPassword(string password) =>
        Application.Helpers.PasswordHelper.Hash(password);

    public static bool VerifyPassword(string password, string hash) =>
        Application.Helpers.PasswordHelper.Verify(password, hash);
}

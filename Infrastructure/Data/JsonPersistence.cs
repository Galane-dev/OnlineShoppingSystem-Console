using System.Text.Json;
using System.Text.Json.Serialization;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Infrastructure.Data;

/// <summary>
/// Handles loading and saving all application data to/from JSON files.
/// Each collection is stored in its own file under the /data directory.
/// </summary>
public class JsonPersistence
{
    private readonly string _dataDir;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new UserJsonConverter() }
    };

    public JsonPersistence(string dataDirectory = "data")
    {
        _dataDir = dataDirectory;
        Directory.CreateDirectory(_dataDir);
    }

    // ── File paths ─────────────────────────────────────────────────────────────

    private string UsersFile    => Path.Combine(_dataDir, "users.json");
    private string ProductsFile => Path.Combine(_dataDir, "products.json");
    private string OrdersFile   => Path.Combine(_dataDir, "orders.json");
    private string PaymentsFile => Path.Combine(_dataDir, "payments.json");
    private string SequenceFile => Path.Combine(_dataDir, "sequences.json");

    // ── Save ───────────────────────────────────────────────────────────────────

    /// <summary>Persists all store collections and ID sequences to disk.</summary>
    public void SaveAll(DataStore store)
    {
        WriteJson(UsersFile, store.Users);
        WriteJson(ProductsFile, store.Products);
        WriteJson(OrdersFile, store.Orders);
        WriteJson(PaymentsFile, store.Payments);
        WriteJson(SequenceFile, store.GetSequences());
    }

    // ── Load ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads persisted data into the store. Returns false if no data files exist
    /// (first run), so the caller can seed defaults.
    /// </summary>
    public bool TryLoadAll(DataStore store)
    {
        if (!File.Exists(UsersFile)) return false;

        var users    = ReadJson<List<User>>(UsersFile);
        var products = ReadJson<List<Product>>(ProductsFile);
        var orders   = ReadJson<List<Order>>(OrdersFile);
        var payments = ReadJson<List<Payment>>(PaymentsFile);
        var seqs     = ReadJson<SequenceData>(SequenceFile);

        if (users == null || products == null) return false;

        // Rehydrate reviews back onto products (stored on product, not separately)
        if (products != null)
        {
            store.Products.AddRange(products);
        }

        if (users != null)
        {
            // Reattach orders and reviews to customer objects
            foreach (var user in users)
            {
                if (user is Customer customer)
                {
                    customer.OrderHistory = orders?
                        .Where(o => o.CustomerId == customer.Id)
                        .ToList() ?? new();
                }
            }
            store.Users.AddRange(users);
        }

        if (orders   != null) store.Orders.AddRange(orders);
        if (payments != null) store.Payments.AddRange(payments);
        if (seqs     != null) store.RestoreSequences(seqs);

        return true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void WriteJson<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(path, json);
    }

    private T? ReadJson<T>(string path)
    {
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}

/// <summary>Snapshot of all ID sequences for persistence.</summary>
public class SequenceData
{
    public int UserId    { get; set; }
    public int ProductId { get; set; }
    public int OrderId   { get; set; }
    public int PaymentId { get; set; }
    public int ReviewId  { get; set; }
}

/// <summary>
/// Polymorphic JSON converter for the User hierarchy.
/// Reads/writes a "$type" discriminator field to distinguish Customer from Administrator.
/// </summary>
public class UserJsonConverter : JsonConverter<User>
{
    public override User? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc   = JsonDocument.ParseValue(ref reader);
        var root        = doc.RootElement;
        var typeDiscrim = root.GetProperty("$type").GetString();

        var json = root.GetRawText();

        // Use a fresh options without this converter to avoid infinite recursion
        var innerOptions = new JsonSerializerOptions(options);
        innerOptions.Converters.Remove(innerOptions.Converters.First(c => c is UserJsonConverter));

        return typeDiscrim switch
        {
            "Customer"      => JsonSerializer.Deserialize<Customer>(json, innerOptions),
            "Administrator" => JsonSerializer.Deserialize<Administrator>(json, innerOptions),
            _ => throw new JsonException($"Unknown user type: {typeDiscrim}")
        };
    }

    public override void Write(Utf8JsonWriter writer, User value, JsonSerializerOptions options)
    {
        var innerOptions = new JsonSerializerOptions(options);
        innerOptions.Converters.Remove(innerOptions.Converters.First(c => c is UserJsonConverter));

        writer.WriteStartObject();
        writer.WriteString("$type", value.Role);

        // Serialize all properties of the concrete type
        var json = value switch
        {
            Customer c      => JsonSerializer.SerializeToDocument(c, innerOptions),
            Administrator a => JsonSerializer.SerializeToDocument(a, innerOptions),
            _ => throw new JsonException("Unknown user type")
        };

        foreach (var prop in json.RootElement.EnumerateObject())
            prop.WriteTo(writer);

        writer.WriteEndObject();
    }
}

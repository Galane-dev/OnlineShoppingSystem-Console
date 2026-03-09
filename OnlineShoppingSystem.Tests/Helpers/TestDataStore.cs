using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Tests.Helpers;

/// <summary>
/// A DataStore subclass that skips JSON persistence entirely.
/// Each test gets a fresh instance with a known, minimal data set
/// so tests are fast, isolated, and deterministic.
/// </summary>
public class TestDataStore : DataStore
{
    // Expose the path-less constructor that skips loading/saving
    public TestDataStore() : base(dataDirectory: null!) { }
}

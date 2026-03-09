using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for Users backed by the in-memory DataStore.
/// All mutation methods call Save() so callers don't need to manage persistence.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DataStore _store;

    public UserRepository(DataStore store)
    {
        _store = store;
    }

    public User? GetById(int id) =>
        _store.Users.FirstOrDefault(u => u.Id == id);

    public User? GetByUsername(string username) =>
        _store.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public List<User> GetAll() => _store.Users.ToList();

    /// <summary>Returns administrators whose accounts are awaiting approval.</summary>
    public List<Administrator> GetPendingAdmins() =>
        _store.Users
            .OfType<Administrator>()
            .Where(a => !a.IsApproved)
            .OrderBy(a => a.RegisteredAt)
            .ToList();

    public void Add(User user)
    {
        _store.Users.Add(user);
        _store.Save();
    }

    public void Save() => _store.Save();
}

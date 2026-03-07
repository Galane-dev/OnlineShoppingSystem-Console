using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Handles user registration and authentication against the in-memory data store.
/// </summary>
public class AuthService : IAuthService
{
    private readonly DataStore _store;

    public AuthService(DataStore store)
    {
        _store = store;
    }

    /// <summary>Attempts login; returns the matching User or null on failure.</summary>
    public User? Login(string username, string password)
    {
        var user = _store.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null) return null;

        return DataStore.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    /// <summary>Registers a new customer account and adds it to the store.</summary>
    public Customer RegisterCustomer(string username, string email, string password, string fullName)
    {
        if (UsernameExists(username))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var customer = new Customer
        {
            Id = _store.NextUserId(),
            Username = username,
            Email = email,
            PasswordHash = DataStore.HashPassword(password),
            FullName = fullName,
            WalletBalance = 0
        };

        customer.Cart = new Cart { CustomerId = customer.Id };
        _store.Users.Add(customer);
        _store.Save();
        return customer;
    }

    public bool UsernameExists(string username) =>
        _store.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
}

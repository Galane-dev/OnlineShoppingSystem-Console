using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Handles user registration, authentication, and account self-service
/// (name updates and password changes).
/// </summary>
public class AuthService : IAuthService
{
    private readonly DataStore _store;

    public AuthService(DataStore store)
    {
        _store = store;
    }

    // ── Authentication ─────────────────────────────────────────────────────────

    /// <summary>Attempts login; returns the matching User or null on failure.</summary>
    public User? Login(string username, string password)
    {
        var user = _store.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null) return null;

        return PasswordHelper.Verify(password, user.PasswordHash) ? user : null;
    }

    // ── Registration ───────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a new customer. Validates username uniqueness and password strength
    /// before persisting.
    /// </summary>
    public Customer RegisterCustomer(string username, string email, string password, string fullName)
    {
        if (UsernameExists(username))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var strengthError = PasswordHelper.GetStrengthError(password);
        if (strengthError != null)
            throw new InvalidOperationException(strengthError);

        var customer = new Customer
        {
            Id           = _store.NextUserId(),
            Username     = username,
            Email        = email,
            PasswordHash = PasswordHelper.Hash(password),
            FullName     = fullName,
            WalletBalance = 0
        };

        customer.Cart = new Cart { CustomerId = customer.Id };
        _store.Users.Add(customer);
        _store.Save();
        return customer;
    }

    public bool UsernameExists(string username) =>
        _store.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    // ── Account management ─────────────────────────────────────────────────────

    /// <summary>Updates the user's display name and persists the change.</summary>
    public void UpdateFullName(User user, string newFullName)
    {
        if (string.IsNullOrWhiteSpace(newFullName))
            throw new ArgumentException("Full name cannot be empty.");

        user.FullName = newFullName.Trim();
        _store.Save();
    }

    /// <summary>
    /// Changes the password after verifying the current one and checking
    /// that the new password meets strength requirements.
    /// </summary>
    public void ChangePassword(User user, string currentPassword, string newPassword)
    {
        if (!PasswordHelper.Verify(currentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        var strengthError = PasswordHelper.GetStrengthError(newPassword);
        if (strengthError != null)
            throw new InvalidOperationException(strengthError);

        user.PasswordHash = PasswordHelper.Hash(newPassword);
        _store.Save();
    }
}


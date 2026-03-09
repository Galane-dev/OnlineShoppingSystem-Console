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
    /// Registers a new customer. Validates username uniqueness and password strength,
    /// then stores the security question and a hash of the (normalised) answer.
    /// </summary>
    public Customer RegisterCustomer(string username, string email, string password, string fullName,
                                     string securityQuestion, string securityAnswer)
    {
        if (UsernameExists(username))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var strengthError = PasswordHelper.GetStrengthError(password);
        if (strengthError != null)
            throw new InvalidOperationException(strengthError);

        if (string.IsNullOrWhiteSpace(securityQuestion))
            throw new ArgumentException("A security question is required.");

        if (string.IsNullOrWhiteSpace(securityAnswer))
            throw new ArgumentException("A security answer is required.");

        var customer = new Customer
        {
            Id = _store.NextUserId(),
            Username = username,
            Email = email,
            PasswordHash = PasswordHelper.Hash(password),
            FullName = fullName,
            SecurityQuestion = securityQuestion.Trim(),
            SecurityAnswerHash = HashSecurityAnswer(securityAnswer),
            WalletBalance = 0
        };

        customer.Cart = new Cart { CustomerId = customer.Id };
        _store.Users.Add(customer);
        _store.Save();
        return customer;
    }

    public bool UsernameExists(string username) =>
        _store.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the User with the given username, or null if not found.</summary>
    public User? FindByUsername(string username) =>
        _store.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

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

    /// <summary>
    /// Resets the password for the given username after verifying the security answer.
    /// The answer comparison is case-insensitive and whitespace-trimmed.
    /// </summary>
    public void ResetPassword(string username, string securityAnswer, string newPassword)
    {
        var user = _store.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No account found with that username.");

        if (HashSecurityAnswer(securityAnswer) != user.SecurityAnswerHash)
            throw new InvalidOperationException("Security answer is incorrect.");

        var strengthError = PasswordHelper.GetStrengthError(newPassword);
        if (strengthError != null)
            throw new InvalidOperationException(strengthError);

        user.PasswordHash = PasswordHelper.Hash(newPassword);
        _store.Save();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises the answer (trim + lowercase) then hashes it, so comparisons
    /// are case-insensitive regardless of how the user typed the answer.
    /// </summary>
    private static string HashSecurityAnswer(string answer) =>
        PasswordHelper.Hash(answer.Trim().ToLowerInvariant());
}


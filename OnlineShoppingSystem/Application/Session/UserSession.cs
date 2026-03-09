using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Application.Session;

/// <summary>
/// Singleton that holds the currently authenticated user for the duration
/// of a login session. Cleared automatically on logout.
///
/// Singleton pattern: only one instance exists for the entire application
/// lifetime, enforced via a private constructor and a static accessor.
/// </summary>
public sealed class UserSession
{
    // ── Singleton infrastructure ───────────────────────────────────────────────

    private static readonly UserSession _instance = new();

    /// <summary>The single global session instance.</summary>
    public static UserSession Current => _instance;

    // Private constructor prevents external instantiation
    private UserSession() { }

    // ── Session state ──────────────────────────────────────────────────────────

    /// <summary>The currently logged-in user, or null when no one is signed in.</summary>
    public User? LoggedInUser { get; private set; }

    /// <summary>True when a user is actively signed in.</summary>
    public bool IsLoggedIn => LoggedInUser != null;

    /// <summary>Convenience accessor — returns the user cast as Customer, or null.</summary>
    public Customer? CurrentCustomer => LoggedInUser as Customer;

    /// <summary>Convenience accessor — returns the user cast as Administrator, or null.</summary>
    public Administrator? CurrentAdmin => LoggedInUser as Administrator;

    // ── Session lifecycle ──────────────────────────────────────────────────────

    /// <summary>Starts a session for the given user.</summary>
    public void Login(User user)
    {
        LoggedInUser = user ?? throw new ArgumentNullException(nameof(user));
    }

    /// <summary>Ends the current session and clears all session state.</summary>
    public void Logout()
    {
        LoggedInUser = null;
    }
}

using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Handles user registration (customer and admin), authentication,
/// admin approval, and account self-service operations.
/// Depends on IUserRepository — never accesses DataStore directly.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly DataStore       _store; // needed only for NextUserId()

    public AuthService(IUserRepository users, DataStore store)
    {
        _users = users;
        _store = store;
    }

    // ── Authentication ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts login. Returns null if credentials are wrong.
    /// Throws if an admin account exists but has not yet been approved.
    /// </summary>
    public User? Login(string username, string password)
    {
        var user = _users.GetByUsername(username);
        if (user == null) return null;

        if (!PasswordHelper.Verify(password, user.PasswordHash)) return null;

        // Block unapproved admins from logging in
        if (user is Administrator { IsApproved: false })
            throw new InvalidOperationException(
                "Your administrator account is pending approval by an existing admin.");

        return user;
    }

    // ── Customer Registration ──────────────────────────────────────────────────

    /// <summary>
    /// Registers a new customer. Validates uniqueness and password strength,
    /// then stores the security question and a hash of the normalised answer.
    /// </summary>
    public Customer RegisterCustomer(string username, string email, string password,
                                     string fullName, string securityQuestion, string securityAnswer)
    {
        ValidateRegistrationInputs(username, password, securityQuestion, securityAnswer);

        var customer = new Customer
        {
            Id                 = _store.NextUserId(),
            Username           = username,
            Email              = email,
            PasswordHash       = PasswordHelper.Hash(password),
            FullName           = fullName,
            SecurityQuestion   = securityQuestion.Trim(),
            SecurityAnswerHash = HashSecurityAnswer(securityAnswer),
            WalletBalance      = 0
        };

        customer.Cart = new Cart { CustomerId = customer.Id };
        _users.Add(customer);
        return customer;
    }

    // ── Admin Registration ─────────────────────────────────────────────────────

    /// <summary>
    /// Registers a new administrator in a pending state.
    /// The account cannot be used until an existing admin approves it.
    /// </summary>
    public Administrator RegisterAdmin(string username, string email, string password,
                                       string fullName, string department,
                                       string securityQuestion, string securityAnswer)
    {
        ValidateRegistrationInputs(username, password, securityQuestion, securityAnswer);

        var admin = new Administrator
        {
            Id                 = _store.NextUserId(),
            Username           = username,
            Email              = email,
            PasswordHash       = PasswordHelper.Hash(password),
            FullName           = fullName,
            Department         = department.Trim(),
            SecurityQuestion   = securityQuestion.Trim(),
            SecurityAnswerHash = HashSecurityAnswer(securityAnswer),
            IsApproved         = false   // must be approved before first login
        };

        _users.Add(admin);
        return admin;
    }

    // ── Admin Approval ─────────────────────────────────────────────────────────

    /// <summary>Returns all administrator accounts that are awaiting approval.</summary>
    public List<Administrator> GetPendingAdmins() =>
        _users.GetPendingAdmins();

    /// <summary>
    /// Approves a pending admin account so the user can log in.
    /// Throws if the target account is not a pending admin.
    /// </summary>
    public void ApproveAdmin(int adminId)
    {
        var user = _users.GetById(adminId)
            ?? throw new InvalidOperationException("User not found.");

        if (user is not Administrator admin)
            throw new InvalidOperationException("The specified user is not an administrator.");

        if (admin.IsApproved)
            throw new InvalidOperationException($"'{admin.FullName}' is already approved.");

        admin.IsApproved = true;
        _users.Save();
    }

    // ── Lookup ─────────────────────────────────────────────────────────────────

    public bool UsernameExists(string username) =>
        _users.GetByUsername(username) != null;

    /// <summary>Returns the User with the given username, or null if not found.</summary>
    public User? FindByUsername(string username) =>
        _users.GetByUsername(username);

    // ── Account management ─────────────────────────────────────────────────────

    /// <summary>Updates the user's display name and persists the change.</summary>
    public void UpdateFullName(User user, string newFullName)
    {
        if (string.IsNullOrWhiteSpace(newFullName))
            throw new ArgumentException("Full name cannot be empty.");

        user.FullName = newFullName.Trim();
        _users.Save();
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
        _users.Save();
    }

    /// <summary>
    /// Resets the password after verifying the security answer.
    /// The answer comparison is case-insensitive and whitespace-trimmed.
    /// </summary>
    public void ResetPassword(string username, string securityAnswer, string newPassword)
    {
        var user = _users.GetByUsername(username)
            ?? throw new InvalidOperationException("No account found with that username.");

        if (HashSecurityAnswer(securityAnswer) != user.SecurityAnswerHash)
            throw new InvalidOperationException("Security answer is incorrect.");

        var strengthError = PasswordHelper.GetStrengthError(newPassword);
        if (strengthError != null)
            throw new InvalidOperationException(strengthError);

        user.PasswordHash = PasswordHelper.Hash(newPassword);
        _users.Save();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void ValidateRegistrationInputs(string username, string password,
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
    }

    /// <summary>
    /// Normalises the answer (trim + lowercase) before hashing so comparisons
    /// are case-insensitive regardless of how the user typed the answer.
    /// </summary>
    private static string HashSecurityAnswer(string answer) =>
        PasswordHelper.Hash(answer.Trim().ToLowerInvariant());
}

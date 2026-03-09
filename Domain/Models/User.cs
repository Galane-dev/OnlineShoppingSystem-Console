namespace OnlineShoppingSystem.Domain.Models;

/// <summary>
/// Abstract base class representing a system user with authentication credentials.
/// </summary>
public abstract class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.Now;

    /// <summary>The security question chosen by the user during registration.</summary>
    public string SecurityQuestion { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the security answer (lowercased and trimmed before hashing).</summary>
    public string SecurityAnswerHash { get; set; } = string.Empty;

    /// <summary>Returns the role label for this user type.</summary>
    public abstract string Role { get; }

    public override string ToString() => $"{FullName} ({Username}) - {Role}";
}

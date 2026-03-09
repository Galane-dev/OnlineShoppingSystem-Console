using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Application.Helpers;

/// <summary>
/// Centralises all password concerns: hashing, verification, and strength validation.
/// Uses SHA-256 with a fixed application salt — suitable for this simulation.
/// </summary>
public static class PasswordHelper
{
    // A fixed salt appended before hashing. In production you would store a
    // per-user random salt alongside the hash.
    private const string AppSalt = "OSS_2025_SALT!@#";

    // ── Hashing ────────────────────────────────────────────────────────────────

    /// <summary>Returns the SHA-256 hex digest of <paramref name="password"/> + salt.</summary>
    public static string Hash(string password)
    {
        var bytes  = Encoding.UTF8.GetBytes(password + AppSalt);
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>Returns true when <paramref name="password"/> matches <paramref name="hash"/>.</summary>
    public static bool Verify(string password, string hash) =>
        Hash(password) == hash;

    // ── Strength validation ────────────────────────────────────────────────────

    /// <summary>
    /// Validates password strength. Returns null when the password is acceptable;
    /// otherwise returns a human-readable rejection reason.
    /// Rules: ≥ 6 characters, at least one uppercase, one lowercase, one digit, one symbol.
    /// </summary>
    public static string? GetStrengthError(string password)
    {
        if (password.Length < 6)
            return "Password must be at least 6 characters long.";

        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter (A–Z).";

        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter (a–z).";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one number (0–9).";

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return "Password must contain at least one symbol (e.g. ! @ # $ % &).";

        return null; // password is acceptable
    }

    /// <summary>Returns true if the password meets all strength requirements.</summary>
    public static bool IsStrong(string password) => GetStrengthError(password) == null;
}

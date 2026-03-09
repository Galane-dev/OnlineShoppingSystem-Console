using FluentAssertions;
using OnlineShoppingSystem.Application.Helpers;

namespace OnlineShoppingSystem.Tests;

/// <summary>Tests for password hashing and strength validation.</summary>
public class PasswordHelperTests
{
    // ── Hashing ────────────────────────────────────────────────────────────────

    [Fact]
    public void Hash_SamePassword_ReturnsSameHash()
    {
        var hash1 = PasswordHelper.Hash("Password1!");
        var hash2 = PasswordHelper.Hash("Password1!");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_DifferentPasswords_ReturnsDifferentHashes()
    {
        var hash1 = PasswordHelper.Hash("Password1!");
        var hash2 = PasswordHelper.Hash("Password2!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHelper.Hash("Password1!");

        PasswordHelper.Verify("Password1!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHelper.Hash("Password1!");

        PasswordHelper.Verify("WrongPass1!", hash).Should().BeFalse();
    }

    // ── Strength validation ────────────────────────────────────────────────────

    [Fact]
    public void GetStrengthError_ValidPassword_ReturnsNull()
    {
        PasswordHelper.GetStrengthError("Secure1!").Should().BeNull();
    }

    [Theory]
    [InlineData("Ab1!",      "at least 6")]   // too short
    [InlineData("password1!","uppercase")]     // no uppercase
    [InlineData("PASSWORD1!","lowercase")]     // no lowercase
    [InlineData("Password!!", "number")]       // no digit
    [InlineData("Password1",  "symbol")]       // no symbol
    public void GetStrengthError_WeakPassword_ReturnsDescriptiveMessage(
        string password, string expectedFragment)
    {
        var error = PasswordHelper.GetStrengthError(password);

        error.Should().NotBeNull();
        error!.ToLower().Should().Contain(expectedFragment.ToLower());
    }
}

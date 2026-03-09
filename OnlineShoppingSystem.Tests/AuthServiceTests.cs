using FluentAssertions;
using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Tests;

/// <summary>
/// Tests for AuthService: registration, login, account self-service,
/// and security-question-based password reset.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly TestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    // ── Registration ───────────────────────────────────────────────────────────

    [Fact]
    public void RegisterCustomer_ValidData_ReturnsCustomerWithCorrectDetails()
    {
        var customer = _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", "Secure1!", "Alice Smith",
            "First pet?", "fluffy");

        customer.Username.Should().Be("alice");
        customer.FullName.Should().Be("Alice Smith");
        customer.Email.Should().Be("alice@test.com");
        customer.Role.Should().Be("Customer");
    }

    [Fact]
    public void RegisterCustomer_ValidData_HashesPasswordAndAnswer()
    {
        var customer = _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", "Secure1!", "Alice Smith",
            "First pet?", "fluffy");

        // Raw values must never be stored
        customer.PasswordHash.Should().NotBe("Secure1!");
        customer.SecurityAnswerHash.Should().NotBe("fluffy");
        customer.SecurityQuestion.Should().Be("First pet?");
    }

    [Fact]
    public void RegisterCustomer_DuplicateUsername_Throws()
    {
        _fx.CreateCustomer("alice");

        var act = () => _fx.AuthService.RegisterCustomer(
            "alice", "other@test.com", "Secure1!", "Other",
            "First pet?", "fluffy");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already taken*");
    }

    [Theory]
    [InlineData("weak")]          // too short, no uppercase/digit/symbol
    [InlineData("alllowercase1!")] // no uppercase
    [InlineData("ALLUPPERCASE1!")] // no lowercase
    [InlineData("NoDigits!!")]     // no digit
    [InlineData("NoSymbol1")]      // no symbol
    public void RegisterCustomer_WeakPassword_Throws(string weakPassword)
    {
        var act = () => _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", weakPassword, "Alice",
            "First pet?", "fluffy");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisterCustomer_AddsCustomerToStore()
    {
        _fx.CreateCustomer("alice");

        _fx.Store.Users.OfType<Customer>()
            .Should().ContainSingle(u => u.Username == "alice");
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Login_CorrectCredentials_ReturnsUser()
    {
        _fx.CreateCustomer("alice");

        var result = _fx.AuthService.Login("alice", "Password1!");

        result.Should().NotBeNull();
        result!.Username.Should().Be("alice");
    }

    [Fact]
    public void Login_WrongPassword_ReturnsNull()
    {
        _fx.CreateCustomer("alice");

        _fx.AuthService.Login("alice", "WrongPass1!").Should().BeNull();
    }

    [Fact]
    public void Login_UnknownUsername_ReturnsNull()
    {
        _fx.AuthService.Login("nobody", "Password1!").Should().BeNull();
    }

    [Fact]
    public void Login_IsCaseInsensitiveOnUsername()
    {
        _fx.CreateCustomer("alice");

        _fx.AuthService.Login("ALICE", "Password1!").Should().NotBeNull();
    }

    // ── UsernameExists / FindByUsername ────────────────────────────────────────

    [Fact]
    public void UsernameExists_KnownUser_ReturnsTrue()
    {
        _fx.CreateCustomer("alice");

        _fx.AuthService.UsernameExists("alice").Should().BeTrue();
    }

    [Fact]
    public void UsernameExists_UnknownUser_ReturnsFalse()
    {
        _fx.AuthService.UsernameExists("nobody").Should().BeFalse();
    }

    [Fact]
    public void FindByUsername_KnownUser_ReturnsUser()
    {
        _fx.CreateCustomer("alice");

        _fx.AuthService.FindByUsername("alice").Should().NotBeNull();
    }

    [Fact]
    public void FindByUsername_UnknownUser_ReturnsNull()
    {
        _fx.AuthService.FindByUsername("nobody").Should().BeNull();
    }

    // ── UpdateFullName ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateFullName_ValidName_UpdatesUser()
    {
        var customer = _fx.CreateCustomer("alice");

        _fx.AuthService.UpdateFullName(customer, "Alice Updated");

        customer.FullName.Should().Be("Alice Updated");
    }

    [Fact]
    public void UpdateFullName_EmptyName_Throws()
    {
        var customer = _fx.CreateCustomer("alice");

        var act = () => _fx.AuthService.UpdateFullName(customer, "   ");

        act.Should().Throw<ArgumentException>();
    }

    // ── ChangePassword ─────────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_CorrectCurrentPassword_UpdatesHash()
    {
        var customer  = _fx.CreateCustomer("alice");
        var oldHash   = customer.PasswordHash;

        _fx.AuthService.ChangePassword(customer, "Password1!", "NewSecure2@");

        customer.PasswordHash.Should().NotBe(oldHash);
        PasswordHelper.Verify("NewSecure2@", customer.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public void ChangePassword_WrongCurrentPassword_Throws()
    {
        var customer = _fx.CreateCustomer("alice");

        var act = () => _fx.AuthService.ChangePassword(customer, "WrongPass1!", "NewSecure2@");

        act.Should().Throw<InvalidOperationException>().WithMessage("*incorrect*");
    }

    [Fact]
    public void ChangePassword_WeakNewPassword_Throws()
    {
        var customer = _fx.CreateCustomer("alice");

        var act = () => _fx.AuthService.ChangePassword(customer, "Password1!", "weak");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── ResetPassword ──────────────────────────────────────────────────────────

    [Fact]
    public void ResetPassword_CorrectAnswer_UpdatesPassword()
    {
        _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", "OldPass1!", "Alice",
            "First pet?", "Fluffy");

        // Answer comparison must be case-insensitive
        _fx.AuthService.ResetPassword("alice", "fluffy", "NewSecure2@");

        var result = _fx.AuthService.Login("alice", "NewSecure2@");
        result.Should().NotBeNull();
    }

    [Fact]
    public void ResetPassword_WrongAnswer_Throws()
    {
        _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", "OldPass1!", "Alice",
            "First pet?", "fluffy");

        var act = () => _fx.AuthService.ResetPassword("alice", "wrong", "NewSecure2@");

        act.Should().Throw<InvalidOperationException>().WithMessage("*incorrect*");
    }

    [Fact]
    public void ResetPassword_UnknownUsername_Throws()
    {
        var act = () => _fx.AuthService.ResetPassword("nobody", "fluffy", "NewSecure2@");

        act.Should().Throw<InvalidOperationException>().WithMessage("*No account*");
    }

    [Fact]
    public void ResetPassword_WeakNewPassword_Throws()
    {
        _fx.AuthService.RegisterCustomer(
            "alice", "alice@test.com", "OldPass1!", "Alice",
            "First pet?", "fluffy");

        var act = () => _fx.AuthService.ResetPassword("alice", "fluffy", "weak");

        act.Should().Throw<InvalidOperationException>();
    }
}

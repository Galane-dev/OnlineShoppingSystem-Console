using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Entry-point menu handling registration, login, and application exit.
/// </summary>
public class MainMenu
{
    private readonly AuthService _authService;
    private readonly CustomerMenu _customerMenu;
    private readonly AdminMenu _adminMenu;

    public MainMenu(AuthService authService, CustomerMenu customerMenu, AdminMenu adminMenu)
    {
        _authService = authService;
        _customerMenu = customerMenu;
        _adminMenu = adminMenu;
    }

    public void Run()
    {
        PrintBanner();

        while (true)
        {
            Console.Clear();
            PrintBanner();
            ConsoleHelper.WriteHeader("Welcome");
            ConsoleHelper.WriteMenuOption(1, "Register",   "Create a new customer account");
            ConsoleHelper.WriteMenuOption(2, "Login",      "Sign in to your account");
            ConsoleHelper.WriteMenuOption(0, "Exit");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 2);

            switch (choice)
            {
                case 1: Register(); break;
                case 2: Login();    break;
                case 0:
                    ConsoleHelper.WriteInfo("Thank you for shopping with us. Goodbye!");
                    return;
            }
        }
    }

    private void Register()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Customer Registration");

        try
        {
            var username = ConsoleHelper.ReadRequiredInput("Username");

            if (_authService.UsernameExists(username))
            {
                ConsoleHelper.WriteError($"Username '{username}' is already taken. Please choose another.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            var fullName = ConsoleHelper.ReadRequiredInput("Full Name");
            var email    = ConsoleHelper.ReadRequiredInput("Email");

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Password requirements: 6+ characters, uppercase, lowercase, number, symbol.");
            Console.WriteLine();

            // Loop until the user enters a password that passes strength validation
            string password;
            while (true)
            {
                password = ConsoleHelper.ReadPassword("Password");

                var error = OnlineShoppingSystem.Application.Helpers.PasswordHelper.GetStrengthError(password);
                if (error == null) break;

                ConsoleHelper.WriteWarning(error);
            }

            var customer = _authService.RegisterCustomer(username, email, password, fullName);
            Console.WriteLine();
            ConsoleHelper.WriteSuccess($"Account created! Welcome, {customer.FullName}.");
            ConsoleHelper.WriteInfo("You can now log in with your credentials.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    private void Login()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Login");

        var username = ConsoleHelper.ReadRequiredInput("Username");
        var password = ConsoleHelper.ReadPassword("Password");

        var user = _authService.Login(username, password);

        if (user == null)
        {
            Console.WriteLine();
            ConsoleHelper.WriteError("Invalid username or password.");
            ConsoleHelper.PressEnterToContinue();
            return;
        }

        Console.WriteLine();
        ConsoleHelper.WriteSuccess($"Logged in as {user.FullName}  ({user.Role})");
        ConsoleHelper.PressEnterToContinue();

        if (user is Customer customer)
            _customerMenu.Run(customer);
        else if (user is Administrator admin)
            _adminMenu.Run(admin);
    }

    private static void PrintBanner()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
  ╔═══════════════════════════════════════════════════════╗
  ║                   UniHub Console                      ║
  ║               Student Friendly Prices                 ║
  ╚═══════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }
}

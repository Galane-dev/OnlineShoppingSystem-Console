using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Application.Session;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Presentation.Helpers;

namespace OnlineShoppingSystem.Presentation.Menus;

/// <summary>
/// Entry-point menu: customer registration, admin registration,
/// login, password reset, and exit.
/// Uses the UserSession singleton to track who is logged in.
/// </summary>
public class MainMenu
{
    private readonly AuthService  _authService;
    private readonly CustomerMenu _customerMenu;
    private readonly AdminMenu    _adminMenu;

    public MainMenu(AuthService authService, CustomerMenu customerMenu, AdminMenu adminMenu)
    {
        _authService  = authService;
        _customerMenu = customerMenu;
        _adminMenu    = adminMenu;
    }

    public void Run()
    {
        PrintBanner();

        while (true)
        {
            Console.Clear();
            PrintBanner();
            ConsoleHelper.WriteHeader("Welcome");
            ConsoleHelper.WriteMenuOption(1, "Register as Customer", "Create a new customer account");
            ConsoleHelper.WriteMenuOption(2, "Register as Admin",    "Apply for an administrator account");
            ConsoleHelper.WriteMenuOption(3, "Login",                "Sign in to your account");
            ConsoleHelper.WriteMenuOption(4, "Forgot Password",      "Reset your password via security question");
            ConsoleHelper.WriteMenuOption(0, "Exit");
            Console.WriteLine();

            var choice = ConsoleHelper.ReadInt("Select option", 0, 4);

            switch (choice)
            {
                case 1: RegisterCustomer(); break;
                case 2: RegisterAdmin();    break;
                case 3: Login();            break;
                case 4: ForgotPassword();   break;
                case 0:
                    ConsoleHelper.WriteInfo("Thank you for shopping with us. Goodbye!");
                    return;
            }
        }
    }

    // ── Customer Registration ──────────────────────────────────────────────────

    private void RegisterCustomer()
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

            var password = ReadStrongPassword();

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Set up a security question in case you ever forget your password.");
            Console.WriteLine();

            var (question, answer) = PromptSecurityQuestion();

            var customer = _authService.RegisterCustomer(
                username, email, password, fullName, question, answer);

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

    // ── Admin Registration ─────────────────────────────────────────────────────

    private void RegisterAdmin()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Administrator Registration");
        ConsoleHelper.WriteInfo("Admin accounts require approval from an existing administrator before you can log in.");
        Console.WriteLine();

        try
        {
            var username = ConsoleHelper.ReadRequiredInput("Username");

            if (_authService.UsernameExists(username))
            {
                ConsoleHelper.WriteError($"Username '{username}' is already taken. Please choose another.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            var fullName   = ConsoleHelper.ReadRequiredInput("Full Name");
            var email      = ConsoleHelper.ReadRequiredInput("Email");
            var department = ConsoleHelper.ReadRequiredInput("Department");

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Password requirements: 6+ characters, uppercase, lowercase, number, symbol.");
            Console.WriteLine();

            var password = ReadStrongPassword();

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Set up a security question in case you ever forget your password.");
            Console.WriteLine();

            var (question, answer) = PromptSecurityQuestion();

            var admin = _authService.RegisterAdmin(
                username, email, password, fullName, department, question, answer);

            Console.WriteLine();
            ConsoleHelper.WriteSuccess($"Application submitted for '{admin.FullName}'.");
            ConsoleHelper.WriteInfo("Your account is pending approval. You will be able to log in once an admin approves it.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    private void Login()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Login");

        var username = ConsoleHelper.ReadRequiredInput("Username");
        var password = ConsoleHelper.ReadPassword("Password");

        try
        {
            var user = _authService.Login(username, password);

            if (user == null)
            {
                Console.WriteLine();
                ConsoleHelper.WriteError("Invalid username or password.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            // Store in the singleton session
            UserSession.Current.Login(user);

            Console.WriteLine();
            ConsoleHelper.WriteSuccess($"Logged in as {user.FullName}  ({user.Role})");
            ConsoleHelper.PressEnterToContinue();

            if (user is Customer customer)
                _customerMenu.Run(customer);
            else if (user is Administrator admin)
                _adminMenu.Run(admin);

            // Clear session on return from menu (i.e. logout)
            UserSession.Current.Logout();
        }
        catch (InvalidOperationException ex) // catches unapproved admin attempt
        {
            Console.WriteLine();
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.PressEnterToContinue();
        }
    }

    // ── Forgot Password ────────────────────────────────────────────────────────

    private void ForgotPassword()
    {
        Console.Clear();
        ConsoleHelper.WriteHeader("Reset Password");

        try
        {
            var username = ConsoleHelper.ReadRequiredInput("Username");
            var user     = _authService.FindByUsername(username);

            if (user == null)
            {
                // Same message whether the user exists or not to prevent enumeration attacks
                ConsoleHelper.WriteError("Unable to verify account details.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            Console.WriteLine();
            ConsoleHelper.WriteInfo($"Security question:  {user.SecurityQuestion}");
            Console.WriteLine();

            var answer = ConsoleHelper.ReadRequiredInput("Your answer");

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Password requirements: 6+ characters, uppercase, lowercase, number, symbol.");
            Console.WriteLine();

            var newPassword = ReadStrongPassword("New password");
            var confirm     = ConsoleHelper.ReadPassword("Confirm new password");

            if (newPassword != confirm)
            {
                ConsoleHelper.WriteError("Passwords do not match. No changes were saved.");
                ConsoleHelper.PressEnterToContinue();
                return;
            }

            _authService.ResetPassword(username, answer, newPassword);

            Console.WriteLine();
            ConsoleHelper.WriteSuccess("Password reset successfully. You can now log in with your new password.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.PressEnterToContinue();
    }

    // ── Shared helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a password with masking and loops until it meets strength requirements.
    /// </summary>
    private static string ReadStrongPassword(string prompt = "Password")
    {
        while (true)
        {
            var password = ConsoleHelper.ReadPassword(prompt);
            var error    = PasswordHelper.GetStrengthError(password);
            if (error == null) return password;
            ConsoleHelper.WriteWarning(error);
        }
    }

    /// <summary>
    /// Presents a numbered list of preset security questions and returns
    /// the chosen question and the user's typed answer.
    /// Using presets avoids weak or ambiguous freeform questions.
    /// </summary>
    private static (string Question, string Answer) PromptSecurityQuestion()
    {
        var questions = new[]
        {
            "What was the name of your first pet?",
            "What is your mother's maiden name?",
            "What was the name of your primary school?",
            "What is the name of the city where you were born?",
            "What was the make and model of your first car?",
            "What is your oldest sibling's middle name?",
            "What street did you grow up on?",
        };

        Console.WriteLine("  Choose a security question:");
        Console.WriteLine();

        for (int i = 0; i < questions.Length; i++)
            ConsoleHelper.WriteMenuOption(i + 1, questions[i]);

        Console.WriteLine();

        var choice   = ConsoleHelper.ReadInt("Select question", 1, questions.Length);
        var question = questions[choice - 1];
        var answer   = ConsoleHelper.ReadRequiredInput("Your answer");

        return (question, answer);
    }

    private static void PrintBanner()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
  ╔═══════════════════════════════════════════════════════╗
  ║          ONLINE SHOPPING SYSTEM  v2.0                 ║
  ║              Backend Console Application              ║
  ╚═══════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }
}

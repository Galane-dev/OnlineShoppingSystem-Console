namespace OnlineShoppingSystem.Presentation.Helpers;

/// <summary>
/// Provides reusable console rendering utilities for consistent UI presentation.
/// </summary>
public static class ConsoleHelper
{
    private const int HeaderWidth = 60;

    public static void WriteHeader(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('═', HeaderWidth));
        Console.WriteLine($"  {title.ToUpper()}");
        Console.WriteLine(new string('═', HeaderWidth));
        Console.ResetColor();
    }

    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {message}");
        Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ ERROR: {message}");
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠ {message}");
        Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    public static void WriteSeparator() =>
        Console.WriteLine(new string('-', HeaderWidth));

    public static void PressEnterToContinue()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Press Enter to continue...");
        Console.ResetColor();
        Console.ReadLine();
    }

    public static string ReadInput(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  {prompt}: ");
        Console.ResetColor();
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Prompts the user for input and validates it is non-empty.
    /// Loops until a valid value is entered.
    /// </summary>
    public static string ReadRequiredInput(string prompt)
    {
        while (true)
        {
            var value = ReadInput(prompt);
            if (!string.IsNullOrWhiteSpace(value)) return value;
            WriteWarning("This field is required.");
        }
    }

    /// <summary>
    /// Prompts for an integer and re-prompts if the value cannot be parsed.
    /// Optionally enforces a minimum/maximum range.
    /// </summary>
    public static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (int.TryParse(input, out int value) && value >= min && value <= max)
                return value;

            WriteWarning($"Please enter a valid number between {min} and {max}.");
        }
    }

    /// <summary>
    /// Prompts for a positive decimal and re-prompts on invalid input.
    /// </summary>
    public static decimal ReadDecimal(string prompt, decimal min = 0)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (decimal.TryParse(input, out decimal value) && value >= min)
                return value;

            WriteWarning($"Please enter a valid amount (minimum R{min:F2}).");
        }
    }

    /// <summary>Menu option without description.</summary>
    public static void WriteMenuOption(int number, string label)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  [{number}]");
        Console.ResetColor();
        Console.WriteLine($" {label}");
    }

    /// <summary>Menu option with a dimmed description hint.</summary>
    public static void WriteMenuOption(int number, string label, string description)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  [{number}]");
        Console.ResetColor();
        Console.Write($" {label,-24}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" — {description}");
        Console.ResetColor();
    }

    /// <summary>
    /// Renders a horizontal status bar showing key/value pairs — e.g. wallet balance and cart total.
    /// </summary>
    public static void WriteStatusBar(params (string Key, string Value)[] items)
    {
        Console.Write("  ");
        for (int i = 0; i < items.Length; i++)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{items[i].Key}: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(items[i].Value);
            if (i < items.Length - 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("   |   ");
            }
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void WriteDivider() =>
        Console.WriteLine(new string('─', HeaderWidth));
}

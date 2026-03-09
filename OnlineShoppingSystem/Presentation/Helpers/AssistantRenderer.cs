using OnlineShoppingSystem.Application.Assistant;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Presentation.Helpers;

/// <summary>
/// Renders <see cref="AssistantResponse"/> objects to the console.
/// All display decisions live here — the service layer produces no console output.
/// </summary>
public static class AssistantRenderer
{
    // ── Main render entry point ────────────────────────────────────────────────

    /// <summary>Renders a complete assistant response to the console.</summary>
    public static void Render(AssistantResponse response)
    {
        Console.WriteLine();
        RenderIntro(response.Intro);

        if (response.Items.Any())
        {
            Console.WriteLine();

            foreach (var line in response.Items)
            {
                if (line.DisplayMode == ProductLineMode.Comparison)
                    RenderComparisonLine(line);
                else
                    RenderStandardLine(line);

                Console.WriteLine();
            }
        }

        RenderFooter(response.Footer);
    }

    // ── Intro ──────────────────────────────────────────────────────────────────

    private static void RenderIntro(string intro)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  🤖 ");
        Console.ResetColor();
        Console.WriteLine(intro);
    }

    // ── Standard product line ──────────────────────────────────────────────────

    private static void RenderStandardLine(ProductLine line)
    {
        var p = line.Product;

        // ── Rank + name ─────────────────────────────────────────────────────
        Console.Write($"  ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{line.Rank}. ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(p.Name);
        Console.ResetColor();

        // ── Category badge ───────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [{p.Category}]");
        Console.ResetColor();

        // ── Price + stock ────────────────────────────────────────────────────
        Console.Write($"     ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"R{p.Price:F2}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  ·  Stock: {p.StockQuantity}");

        if (p.AverageRating > 0)
        {
            Console.ResetColor();
            Console.Write("  ·  ");
            RenderStars(p.AverageRating);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" ({p.Reviews.Count} review{(p.Reviews.Count == 1 ? "" : "s")})");
        }

        Console.ResetColor();
        Console.WriteLine();

        // ── Description snippet ──────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"     {Truncate(p.Description, 72)}");
        Console.ResetColor();

        // ── Why it was suggested ─────────────────────────────────────────────
        if (line.Reasons.Any())
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"     ✓ {string.Join("  ·  ", line.Reasons)}");
            Console.ResetColor();
        }

        // ── ID hint ──────────────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"     Use product ID {p.Id} to add this to your cart or wishlist.");
        Console.ResetColor();
    }

    // ── Comparison line ────────────────────────────────────────────────────────

    private static void RenderComparisonLine(ProductLine line)
    {
        var p = line.Product;

        // Header row for comparison
        Console.Write($"  ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{line.Rank}. ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(p.Name);
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [{p.Category}]");
        Console.ResetColor();

        // Comparison table row
        var priceCol   = $"R{p.Price:F2}".PadRight(12);
        var ratingCol  = p.AverageRating > 0
            ? $"{p.AverageRating:F1}★".PadRight(8)
            : "No reviews".PadRight(8);
        var stockCol   = $"Stock: {p.StockQuantity}";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"     {priceCol}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write($"  {ratingCol}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {stockCol}");

        // Description
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"     {Truncate(p.Description, 72)}");
        Console.ResetColor();

        // Reasons
        if (line.Reasons.Any())
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"     ✓ {string.Join("  ·  ", line.Reasons)}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"     Product ID: {p.Id}");
        Console.ResetColor();
    }

    // ── Footer ─────────────────────────────────────────────────────────────────

    private static void RenderFooter(string footer)
    {
        if (string.IsNullOrWhiteSpace(footer)) return;

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  {footer}");
        Console.ResetColor();
    }

    // ── Star rating renderer ───────────────────────────────────────────────────

    private static void RenderStars(double rating)
    {
        var full  = (int)Math.Round(rating);
        var empty = 5 - full;

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(new string('★', full));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('☆', empty));
        Console.ResetColor();
    }

    // ── Chat prompt renderer ───────────────────────────────────────────────────

    /// <summary>
    /// Renders the chat header with example prompts to guide the user.
    /// Called once when the chat screen opens.
    /// </summary>
    public static void RenderChatHeader(string customerName, decimal walletBalance)
    {
        ConsoleHelper.WriteHeader($"AI Shopping Assistant  ·  Hi, {customerName}!");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine("  Ask me anything about our products in plain English.");
        Console.WriteLine();
        Console.WriteLine("  Examples:");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"    · \"With R{walletBalance:F0}, what can I buy?\"");
        Console.WriteLine("    · \"I need a gift for someone who likes fitness\"");
        Console.WriteLine("    · \"Compare your laptops\"");
        Console.WriteLine("    · \"What are your best rated products?\"");
        Console.WriteLine("    · \"Show me electronics under R1000\"");
        Console.WriteLine("    · \"Something portable for travel\"");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Type 'back' or press Enter on an empty line to return to the menu.");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine(new string('─', 60));
    }

    /// <summary>Renders the user's typed message in a chat-bubble style.</summary>
    public static void RenderUserMessage(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  You: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine(new string('─', 60));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
}

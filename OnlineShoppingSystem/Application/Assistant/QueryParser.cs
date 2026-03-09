using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Application.Assistant;

/// <summary>
/// The parsed, structured representation of a user's natural-language shopping query.
/// </summary>
public sealed class ParsedQuery
{
    public QueryIntent  Intent       { get; init; } = QueryIntent.General;
    public decimal?     MaxBudget    { get; init; }
    public decimal?     MinBudget    { get; init; }
    public List<string> Categories   { get; init; } = [];
    public List<string> Keywords     { get; init; } = [];
    public List<string> UseCases     { get; init; } = [];
    public bool         WantsTopRated{ get; init; }
    public bool         WantsNew     { get; init; }
    public string       Raw          { get; init; } = string.Empty;
}

public enum QueryIntent
{
    General,        // open-ended browsing — always returns results
    BudgetBased,    // "with R500, what can I buy"
    CategoryBased,  // "show me fitness products"
    GiftSearch,     // "I need a gift for someone who likes..."
    Comparison,     // "compare the laptops"
    TopRated,       // "what's your best rated product"
    Recommendation  // "what do you recommend for..."
}

/// <summary>
/// Parses a raw user message into a structured <see cref="ParsedQuery"/>.
///
/// Design principles:
///   - Budget extraction requires an ACTUAL NUMBER — words like "with" or "budget"
///     alone never produce a budget value.
///   - Intent is determined AFTER budget/category/signals are extracted, so it
///     is based on what was actually found, not on surface-level word matches.
///   - Ambiguous queries default to General intent so the user always gets results.
/// </summary>
public static class QueryParser
{
    // ── Category keyword map ───────────────────────────────────────────────────

    private static readonly Dictionary<string, string[]> CategorySignals = new()
    {
        ["Electronics"] = [
            "laptop", "computer", "mouse", "keyboard", "usb", "hub", "bluetooth",
            "speaker", "headphone", "tech", "electronic", "device", "gadget",
            "wireless", "cable", "charger", "screen", "monitor", "pc", "audio",
        ],
        ["Fitness"] = [
            "fitness", "gym", "workout", "exercise", "yoga", "mat", "sport",
            "health", "training", "running", "active", "stretch", "pilates",
            "weights", "cardio", "athletic",
        ],
        ["Footwear"] = [
            "shoe", "shoes", "boot", "boots", "sneaker", "sneakers", "footwear",
            "trainers", "feet", "foot", "heel", "sole",
        ],
        ["Books"] = [
            "book", "books", "read", "reading", "novel", "guide", "study", "learn",
            "programming", "coding", "software", "literature", "textbook",
        ],
        ["Appliances"] = [
            "coffee", "appliance", "kitchen", "maker", "brew", "machine",
            "blender", "toaster", "kettle", "microwave",
        ],
        ["Furniture"] = [
            "lamp", "light", "desk", "chair", "table", "furniture", "shelf",
            "shelving", "decor", "cushion", "sofa",
        ],
        ["Accessories"] = [
            "bag", "backpack", "accessory", "accessories", "travel bag", "pack",
            "carry", "luggage", "pouch", "wallet", "strap",
        ],
    };

    // ── Use-case / occasion signals ────────────────────────────────────────────

    private static readonly Dictionary<string, string[]> UseCaseSignals = new()
    {
        ["gift"]    = ["gift", "present", "birthday", "christmas", "anniversary",
                       "for someone", "for my friend", "for my", "for a friend", "giving", "surprise"],
        ["office"]  = ["office", "work", "desk", "productive", "professional", "business", "working from home"],
        ["travel"]  = ["travel", "trip", "holiday", "vacation", "portable", "on the go", "commute"],
        ["home"]    = ["home", "house", "household", "room", "living room", "bedroom"],
        ["student"] = ["student", "study", "school", "university", "college", "learning", "class"],
        ["outdoor"] = ["outdoor", "outside", "nature", "trail", "hike", "hiking", "camping", "camp"],
    };

    // ── Stop words — must NOT include category signals ─────────────────────────
    // Kept intentionally small. Only remove words with zero product-meaning.

    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "by", "from", "is", "it", "its", "be", "as", "was", "are",
        "were", "been", "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "may", "might", "can",
        "i", "me", "my", "we", "you", "your",
        "what", "which", "who", "how", "when", "where",
        "get", "give", "buy", "need", "want", "looking", "find", "show",
        "something", "anything", "everything", "some", "any",
        "please", "help", "tell", "also", "just", "really",
        "good", "great", "nice", "cheap", "affordable",
        "price", "cost", "rand", "rands", "money", "budget",
    ];

    // ── Entry point ────────────────────────────────────────────────────────────

    public static ParsedQuery Parse(string input)
    {
        var text = input.ToLowerInvariant().Trim();

        // Extract signals first — intent is derived from what was actually found
        var maxBudget  = ExtractMaxBudget(text);
        var minBudget  = ExtractMinBudget(text);
        var categories = DetectCategories(text);
        var useCases   = DetectUseCases(text);
        var keywords   = ExtractKeywords(text);

        return new ParsedQuery
        {
            Raw           = input,
            MaxBudget     = maxBudget,
            MinBudget     = minBudget,
            Categories    = categories,
            Keywords      = keywords,
            UseCases      = useCases,
            WantsTopRated = ContainsAny(text, ["best rated", "top rated", "highest rated",
                                               "most popular", "well reviewed", "top products"]),
            WantsNew      = ContainsAny(text, ["new", "latest", "recent", "just arrived", "newest"]),
            Intent        = DeriveIntent(text, maxBudget, minBudget, categories, useCases),
        };
    }

    // ── Intent derivation — ORDER MATTERS, most specific first ────────────────

    /// <summary>
    /// Derives intent from what was ACTUALLY extracted, not from surface words.
    /// This prevents "with good battery life" from becoming BudgetBased.
    /// </summary>
    private static QueryIntent DeriveIntent(
        string text, decimal? maxBudget, decimal? minBudget,
        List<string> categories, List<string> useCases)
    {
        // Comparison: explicit compare language
        if (ContainsAny(text, ["compare", " vs ", "versus", "difference between", "which is better",
                                "which one", "what's the difference"]))
            return QueryIntent.Comparison;

        // Gift: explicit gift/person language
        if (useCases.Contains("gift") ||
            ContainsAny(text, ["gift", "present", "birthday", "for someone", "for my friend"]))
            return QueryIntent.GiftSearch;

        // Top-rated: explicit rating language
        if (ContainsAny(text, ["best rated", "top rated", "highest rated", "most popular",
                                "well reviewed", "top products", "highest review"]))
            return QueryIntent.TopRated;

        // Recommendation: explicit ask for advice
        if (ContainsAny(text, ["recommend", "suggestion", "suggest", "what should i",
                                "what would you", "help me choose", "help me pick",
                                "what's good", "advise", "advice"]))
            return QueryIntent.Recommendation;

        // Budget: ONLY if an actual number was extracted
        if (maxBudget.HasValue || minBudget.HasValue)
            return QueryIntent.BudgetBased;

        // Category: detected a category
        if (categories.Any())
            return QueryIntent.CategoryBased;

        // Default: always returns something useful
        return QueryIntent.General;
    }

    // ── Budget extraction — requires an actual numeric value ──────────────────

    /// <summary>
    /// Extracts a maximum budget. Patterns are ordered most-specific → least-specific.
    /// A budget is ONLY set when a real number is found — never from words alone.
    /// </summary>
    private static decimal? ExtractMaxBudget(string text)
    {
        var patterns = new[]
        {
            // "under R1000", "less than R500", "no more than R300", "up to R800"
            @"(?:under|less than|below|no more than|at most|up to|within|maximum of|max of?)\s*r\s*(\d[\d\s,]*(?:\.\d{1,2})?)",

            // "R500 or less", "R300 budget", "R1000 max"
            @"r\s*(\d[\d\s,]*(?:\.\d{1,2})?)\s*(?:or less|and under|budget|max|maximum|limit|only)",

            // "500 rand or less", "300 rands budget"
            @"(\d[\d\s,]*(?:\.\d{1,2})?)\s*(?:rand|rands|zar)\s*(?:or less|budget|max)?",

            // "with R500", "have R500", "got R500", "spend R500", "I have R500"
            @"(?:with|have|got|spend|spending|budget of|afford|i have)\s+r\s*(\d[\d\s,]*(?:\.\d{1,2})?)",

            // bare "R500" — least specific, matched last
            @"(?<!\d)r\s*(\d{2,}(?:[,\s]\d{3})*(?:\.\d{1,2})?)\b",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && TryParseAmount(match.Groups[1].Value, out var amount))
                return amount;
        }

        return null;
    }

    /// <summary>Extracts a minimum budget from "over R200", "at least R300", "more than R100".</summary>
    private static decimal? ExtractMinBudget(string text)
    {
        var pattern = @"(?:over|above|more than|at least|minimum of?|from)\s*r?\s*(\d[\d\s,]*(?:\.\d{1,2})?)";
        var match   = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

        if (match.Success && TryParseAmount(match.Groups[1].Value, out var amount))
            return amount;

        return null;
    }

    private static bool TryParseAmount(string raw, out decimal result)
    {
        var cleaned = raw.Replace(" ", "").Replace(",", "");
        return decimal.TryParse(cleaned, out result) && result > 0 && result < 1_000_000;
    }

    // ── Category and use-case detection ───────────────────────────────────────

    private static List<string> DetectCategories(string text) =>
        CategorySignals
            .Where(kv => kv.Value.Any(signal => text.Contains(signal)))
            .Select(kv => kv.Key)
            .Distinct()
            .ToList();

    private static List<string> DetectUseCases(string text) =>
        UseCaseSignals
            .Where(kv => kv.Value.Any(signal => text.Contains(signal)))
            .Select(kv => kv.Key)
            .Distinct()
            .ToList();

    // ── Keyword extraction ─────────────────────────────────────────────────────

    /// <summary>
    /// Extracts meaningful words after removing stop words and budget/number tokens.
    /// These are used for fuzzy-matching product names and descriptions.
    /// </summary>
    private static List<string> ExtractKeywords(string text)
    {
        // Remove currency amounts so "R500" doesn't become a keyword
        var cleaned = Regex.Replace(text, @"r?\d[\d,.\s]*", " ");

        return Regex.Replace(cleaned, @"[^\w\s]", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Distinct()
            .ToList();
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static bool ContainsAny(string text, string[] terms) =>
        terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}

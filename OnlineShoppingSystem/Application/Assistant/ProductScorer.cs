using OnlineShoppingSystem.Application.Helpers;
using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Application.Assistant;

/// <summary>
/// A product paired with its relevance score and the human-readable reasons
/// it was selected, used by the response composer to explain recommendations.
/// </summary>
public sealed class ScoredProduct
{
    public Product      Product { get; init; } = null!;
    public double       Score   { get; init; }
    public List<string> Reasons { get; init; } = [];
}

/// <summary>
/// Scores every active, in-stock product against a <see cref="ParsedQuery"/> using a
/// multi-signal weighted algorithm. Higher scores mean stronger relevance.
///
/// Key design decisions:
///   - Keywords are scored INDIVIDUALLY against name/description, not joined into
///     one long string. This means "yoga mat" scores well for "Yoga Mat" even when
///     other unrelated keywords are also present in the query.
///   - BudgetBased intent with no MaxBudget is treated as General (never happens now
///     that QueryParser only sets BudgetBased when a number was actually found).
///   - General intent includes ALL in-stock products so the user always gets results.
///   - Score-zero products are excluded unless intent is General or BudgetBased
///     with no other signals, preventing irrelevant results.
/// </summary>
public static class ProductScorer
{
    // ── Signal weights ─────────────────────────────────────────────────────────

    private const double WeightBudgetFit     = 3.0;
    private const double WeightNameMatch     = 2.5;
    private const double WeightDescMatch     = 1.5;
    private const double WeightCategoryMatch = 2.0;
    private const double WeightUseCaseMatch  = 1.8;
    private const double WeightRating        = 0.8;
    private const double WeightValueForMoney = 1.2;

    // ── Entry point ────────────────────────────────────────────────────────────

    public static List<ScoredProduct> Score(IEnumerable<Product> products, ParsedQuery query)
    {
        var results = new List<ScoredProduct>();

        // Determine whether we need relevance signals to include a product,
        // or whether everything in-budget qualifies (budget-only query).
        var requiresRelevance = query.Keywords.Any()
                             || query.Categories.Any()
                             || query.UseCases.Any()
                             || query.WantsTopRated;

        foreach (var product in products.Where(p => p.IsActive && p.IsInStock))
        {
            // Hard price filters
            if (query.MaxBudget.HasValue && product.Price > query.MaxBudget.Value) continue;
            if (query.MinBudget.HasValue && product.Price < query.MinBudget.Value) continue;

            var (score, reasons) = ComputeScore(product, query);

            // Include if: has a signal score, OR no relevance signals exist (show everything)
            if (score > 0 || !requiresRelevance)
                results.Add(new ScoredProduct { Product = product, Score = score, Reasons = reasons });
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Product.Price)       // tie-break: cheaper first
            .ToList();
    }

    // ── Scoring pipeline ───────────────────────────────────────────────────────

    private static (double Score, List<string> Reasons) ComputeScore(Product product, ParsedQuery query)
    {
        double score = 0;
        var    reasons = new List<string>();

        score += ScoreBudgetFit(product, query, reasons);
        score += ScoreKeywordMatch(product, query, reasons);
        score += ScoreCategoryMatch(product, query, reasons);
        score += ScoreUseCaseMatch(product, query, reasons);
        score += ScoreRating(product, query, reasons);
        score += ScoreValueForMoney(product, query, reasons);

        return (score, reasons);
    }

    // ── Individual signal scorers ──────────────────────────────────────────────

    /// <summary>
    /// Budget fit: tiered bonus based on how far under budget the product is.
    /// Products well under budget get a higher bonus — helps surface great deals.
    /// </summary>
    private static double ScoreBudgetFit(Product product, ParsedQuery query, List<string> reasons)
    {
        if (!query.MaxBudget.HasValue) return 0;

        var ratio = product.Price / query.MaxBudget.Value;

        if (ratio <= 0.40m)
        {
            reasons.Add($"only R{product.Price:F0} of your R{query.MaxBudget:F0} budget");
            return WeightBudgetFit * 1.5;
        }

        if (ratio <= 0.65m)
        {
            reasons.Add("well within budget");
            return WeightBudgetFit * 1.2;
        }

        reasons.Add("fits your budget");
        return WeightBudgetFit;
    }

    /// <summary>
    /// Keyword match: scores each keyword individually against name AND description,
    /// then sums them. This way "yoga mat" scores high for "Yoga Mat" regardless of
    /// how many other keywords are in the query.
    /// </summary>
    private static double ScoreKeywordMatch(Product product, ParsedQuery query, List<string> reasons)
    {
        if (!query.Keywords.Any()) return 0;

        double nameScore = 0;
        double descScore = 0;

        foreach (var keyword in query.Keywords)
        {
            nameScore += FuzzyMatcher.Score(product.Name,        keyword);
            descScore += FuzzyMatcher.Score(product.Description, keyword);
        }

        // Normalise by keyword count so a query with 10 keywords doesn't overwhelm
        // the score relative to a query with 2 keywords
        var keywordCount = (double)query.Keywords.Count;
        nameScore /= keywordCount;
        descScore /= keywordCount;

        var best = Math.Max(nameScore, descScore);
        if (best <= 0) return 0;

        if (nameScore >= 0.7)
            reasons.Add("strong name match");
        else if (best >= 0.4)
            reasons.Add("matches your search terms");

        return (nameScore * WeightNameMatch) + (descScore * WeightDescMatch);
    }

    /// <summary>Full weight when the product's category matches a detected category.</summary>
    private static double ScoreCategoryMatch(Product product, ParsedQuery query, List<string> reasons)
    {
        if (!query.Categories.Any()) return 0;

        var matched = query.Categories.Any(c =>
            product.Category.Equals(c, StringComparison.OrdinalIgnoreCase));

        if (!matched) return 0;

        reasons.Add($"in {product.Category}");
        return WeightCategoryMatch;
    }

    /// <summary>
    /// Use-case match: checks whether the product's name/description contains
    /// keywords that are characteristic of the detected use case.
    /// Each use case has a set of indicator words — more matches = higher score.
    /// </summary>
    private static double ScoreUseCaseMatch(Product product, ParsedQuery query, List<string> reasons)
    {
        if (!query.UseCases.Any()) return 0;

        var combined = $"{product.Name} {product.Description}".ToLowerInvariant();

        // Indicator words per use case — these appear in product data, not in user queries
        var indicators = new Dictionary<string, string[]>
        {
            ["gift"]    = ["premium", "quality", "portable", "lightweight", "stylish", "elegant", "wireless"],
            ["office"]  = ["desk", "office", "professional", "ergonomic", "adjustable", "programmable"],
            ["travel"]  = ["portable", "lightweight", "compact", "water-resistant", "wireless", "battery"],
            ["home"]    = ["home", "kitchen", "room", "adjustable", "programmable", "12-cup"],
            ["student"] = ["guide", "learning", "study", "portable", "lightweight"],
            ["outdoor"] = ["water-resistant", "durable", "trail", "lightweight", "battery"],
        };

        double score = 0;
        foreach (var useCase in query.UseCases)
        {
            if (!indicators.TryGetValue(useCase, out var words)) continue;

            var matchCount = words.Count(w => combined.Contains(w));
            if (matchCount == 0) continue;

            var ratio  = matchCount / (double)words.Length;
            var signal = ratio >= 0.4 ? $"great for {useCase}" : $"suits {useCase} use";
            if (!reasons.Contains(signal)) reasons.Add(signal);

            score += WeightUseCaseMatch * ratio;
        }

        return score;
    }

    /// <summary>
    /// Rating bonus: scaled linearly. Doubles when the user explicitly asks
    /// for top-rated products, making ratings the dominant signal in that case.
    /// </summary>
    private static double ScoreRating(Product product, ParsedQuery query, List<string> reasons)
    {
        if (product.AverageRating <= 0) return 0;

        var ratingScore = (product.AverageRating / 5.0) * WeightRating;

        if (query.WantsTopRated && product.AverageRating >= 4.0)
        {
            reasons.Add($"highly rated {product.AverageRating:F1}★");
            return ratingScore * 3.0;
        }

        if (product.AverageRating >= 4.5)
            reasons.Add($"rated {product.AverageRating:F1}★");

        return ratingScore;
    }

    /// <summary>
    /// Value-for-money: rewards high-rated products that are cheap relative to budget.
    /// Only meaningful when a budget was stated.
    /// </summary>
    private static double ScoreValueForMoney(Product product, ParsedQuery query, List<string> reasons)
    {
        if (!query.MaxBudget.HasValue || product.AverageRating <= 0) return 0;

        var priceFraction = (double)(product.Price / query.MaxBudget.Value);
        var valueRatio    = (product.AverageRating / 5.0) / priceFraction;

        if (valueRatio >= 3.0)
        {
            reasons.Add("excellent value for money");
            return WeightValueForMoney;
        }

        if (valueRatio >= 1.8)
            return WeightValueForMoney * 0.5;

        return 0;
    }
}

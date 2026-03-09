using OnlineShoppingSystem.Domain.Models;

namespace OnlineShoppingSystem.Application.Assistant;

/// <summary>
/// Turns a list of <see cref="ScoredProduct"/> results into a natural,
/// conversational response tailored to the detected query intent.
/// </summary>
public static class ResponseComposer
{
    private const int MaxResults = 5;

    // ── Entry point ────────────────────────────────────────────────────────────

    public static AssistantResponse Compose(ParsedQuery query, List<ScoredProduct> results)
    {
        if (!results.Any())
            return BuildNoResultsResponse(query);

        return query.Intent switch
        {
            QueryIntent.BudgetBased    => ComposeBudgetResponse(query, results),
            QueryIntent.GiftSearch     => ComposeGiftResponse(query, results),
            QueryIntent.Comparison     => ComposeComparisonResponse(query, results),
            QueryIntent.TopRated       => ComposeTopRatedResponse(query, results),
            QueryIntent.CategoryBased  => ComposeCategoryResponse(query, results),
            QueryIntent.Recommendation => ComposeRecommendationResponse(query, results),
            _                          => ComposeGeneralResponse(query, results),
        };
    }

    // ── Intent-specific composers ──────────────────────────────────────────────

    private static AssistantResponse ComposeBudgetResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top      = results.Take(MaxResults).ToList();
        var total    = results.Count;
        var cheapest = results.MinBy(r => r.Product.Price)!.Product;
        var budget   = query.MaxBudget!.Value;

        var intro = $"With R{budget:F0} you can afford {total} item{(total == 1 ? "" : "s")}. " +
                    $"Here are my top {Math.Min(total, MaxResults)} picks:";

        var footer = total > MaxResults
            ? $"💡 {total - MaxResults} more options available — use 'Search Products' to explore them all."
            : $"💡 Best deal: {cheapest.Name} at R{cheapest.Price:F2}.";

        return Build(query.Intent, intro, BuildProductLines(top), footer);
    }

    private static AssistantResponse ComposeGiftResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top = results.Take(MaxResults).ToList();

        var intro = query.Categories.Any()
            ? $"Great gift ideas in {string.Join(" & ", query.Categories)}:"
            : "Here are some thoughtful gift ideas:";

        return Build(query.Intent, intro, BuildProductLines(top),
            "💡 Products with higher ratings tend to make more appreciated gifts!");
    }

    private static AssistantResponse ComposeComparisonResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top = results.Take(4).ToList();

        if (top.Count == 1)
            return Build(query.Intent,
                "I only found one matching product — not enough for a comparison, but here it is:",
                BuildProductLines(top),
                "💡 Try broadening your search to find more products to compare.");

        var cheapest  = top.MinBy(r => r.Product.Price)!.Product;
        var bestRated = top.Where(r => r.Product.AverageRating > 0)
                           .MaxBy(r => r.Product.AverageRating)?.Product;

        var footer = bestRated != null && bestRated.Id != cheapest.Id
            ? $"💡 Cheapest: {cheapest.Name} (R{cheapest.Price:F2}). Best rated: {bestRated.Name} ({bestRated.AverageRating:F1}★)."
            : $"💡 Most affordable: {cheapest.Name} at R{cheapest.Price:F2}.";

        return Build(query.Intent, $"Comparing {top.Count} products:", BuildComparisonLines(top), footer);
    }

    private static AssistantResponse ComposeTopRatedResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top = results
            .OrderByDescending(r => r.Product.AverageRating)
            .ThenByDescending(r => r.Score)
            .Take(MaxResults)
            .ToList();

        var hasRatings = top.Any(r => r.Product.AverageRating > 0);

        var intro = hasRatings
            ? "Our highest-rated products based on customer reviews:"
            : "No reviews yet — here are the most relevant products:";

        return Build(query.Intent, intro, BuildProductLines(top),
            "💡 Ratings come from verified purchases only.");
    }

    private static AssistantResponse ComposeCategoryResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top = results.Take(MaxResults).ToList();
        var cats = string.Join(" & ", query.Categories);

        var footer = results.Count > MaxResults
            ? $"💡 Showing {MaxResults} of {results.Count} results in {cats}."
            : $"💡 {results.Count} product{(results.Count == 1 ? "" : "s")} in {cats}.";

        return Build(query.Intent, $"Here's what we have in {cats}:", BuildProductLines(top), footer);
    }

    private static AssistantResponse ComposeRecommendationResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top     = results.Take(MaxResults).ToList();
        var context = query.UseCases.Any()  ? $" for {string.Join(" and ", query.UseCases)}"
                    : query.Categories.Any()? $" in {string.Join(" & ", query.Categories)}"
                    : "";

        return Build(query.Intent,
            $"My top recommendations{context}:",
            BuildProductLines(top),
            "💡 Add any of these to your wishlist to save them for later.");
    }

    private static AssistantResponse ComposeGeneralResponse(ParsedQuery query, List<ScoredProduct> results)
    {
        var top = results.Take(MaxResults).ToList();

        var footer = results.Count > MaxResults
            ? $"💡 {results.Count} total matches. Try being more specific to narrow things down."
            : "💡 Try asking about budget, category, or use case for more tailored results.";

        return Build(query.Intent, "Here's what I found:", BuildProductLines(top), footer);
    }

    // ── No results ─────────────────────────────────────────────────────────────

    private static AssistantResponse BuildNoResultsResponse(ParsedQuery query)
    {
        var message = query.Intent switch
        {
            QueryIntent.BudgetBased when query.MaxBudget.HasValue =>
                $"Nothing in stock under R{query.MaxBudget:F0} right now. " +
                "Try a higher budget or browse all products.",

            QueryIntent.CategoryBased =>
                $"No products in {string.Join(" or ", query.Categories)} are in stock right now.",

            _ => "I couldn't find products matching that description. " +
                 "Try different keywords or browse all products."
        };

        return Build(query.Intent, message, [],
            "💡 Try: \"with R500 what can I buy\" · \"show me electronics\" · \"best rated products\"");
    }

    // ── Builders ───────────────────────────────────────────────────────────────

    private static AssistantResponse Build(QueryIntent intent, string intro,
                                           List<ProductLine> items, string footer) =>
        new() { Intro = intro, Items = items, Footer = footer, Intent = intent };

    private static List<ProductLine> BuildProductLines(List<ScoredProduct> items) =>
        items.Select((r, i) => new ProductLine
        {
            Rank        = i + 1,
            Product     = r.Product,
            Reasons     = r.Reasons,
            DisplayMode = ProductLineMode.Standard,
        }).ToList();

    private static List<ProductLine> BuildComparisonLines(List<ScoredProduct> items) =>
        items.Select((r, i) => new ProductLine
        {
            Rank        = i + 1,
            Product     = r.Product,
            Reasons     = r.Reasons,
            DisplayMode = ProductLineMode.Comparison,
        }).ToList();
}

// ── Response model (kept in same file — small, tightly coupled) ───────────────

public sealed class AssistantResponse
{
    public string            Intro  { get; init; } = string.Empty;
    public List<ProductLine> Items  { get; init; } = [];
    public string            Footer { get; init; } = string.Empty;
    public QueryIntent       Intent { get; init; }
}

public sealed class ProductLine
{
    public int             Rank        { get; init; }
    public Product         Product     { get; init; } = null!;
    public List<string>    Reasons     { get; init; } = [];
    public ProductLineMode DisplayMode { get; init; }
}

public enum ProductLineMode { Standard, Comparison }

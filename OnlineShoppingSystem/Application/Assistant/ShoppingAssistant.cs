using OnlineShoppingSystem.Domain.Interfaces;

namespace OnlineShoppingSystem.Application.Assistant;

/// <summary>
/// Orchestrates the rule-based AI shopping assistant pipeline:
///
///   Raw user text
///       → QueryParser    (extract intent, budget, categories, keywords)
///       → ContextMerger  (inherit context from previous turn in conversation)
///       → ProductScorer  (score every product against the merged query)
///       → ResponseComposer (build a natural-language response)
///       → AssistantResponse
///
/// Maintains a <see cref="ConversationContext"/> so follow-up questions work
/// naturally, e.g:
///   Turn 1: "Show me fitness gear"     → context: category=Fitness
///   Turn 2: "Which of those are cheap?"→ still scoped to Fitness
///   Turn 3: "under R300"              → budget added, still Fitness
/// </summary>
public sealed class ShoppingAssistant
{
    private readonly IProductRepository _products;
    private ConversationContext         _context = new();

    public ShoppingAssistant(IProductRepository products)
    {
        _products = products;
    }

    /// <summary>
    /// Processes a natural-language query, merges it with conversation context,
    /// and returns a fully composed response. Never throws.
    /// </summary>
    public AssistantResponse Ask(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return BuildEmptyInputResponse();

        try
        {
            // Stage 1 — Parse current turn
            var query = QueryParser.Parse(userInput);

            // Stage 2 — Merge with conversation context for follow-up awareness
            var mergedQuery = _context.Merge(query);

            // Stage 3 — Score products
            var allProducts = _products.GetActive();
            var scored      = ProductScorer.Score(allProducts, mergedQuery);

            // Stage 4 — Compose response
            var response = ResponseComposer.Compose(mergedQuery, scored);

            // Stage 5 — Update context for the next turn
            _context = _context.Update(mergedQuery);

            return response;
        }
        catch (Exception ex)
        {
            return new AssistantResponse
            {
                Intro  = "Sorry, something went wrong processing your request.",
                Items  = [],
                Footer = $"Details: {ex.Message}",
                Intent = QueryIntent.General,
            };
        }
    }

    /// <summary>Resets conversation context (called when user leaves the assistant screen).</summary>
    public void ResetContext() => _context = new();

    private static AssistantResponse BuildEmptyInputResponse() => new()
    {
        Intro  = "What are you looking for? Ask me anything about our products.",
        Items  = [],
        Footer = "💡 Try: \"with R500 what can I buy?\"  ·  \"show me fitness gear\"  ·  \"best rated products\"",
        Intent = QueryIntent.General,
    };
}

// ── Conversation context ───────────────────────────────────────────────────────

/// <summary>
/// Carries forward signals from previous turns so follow-up questions work naturally.
///
/// Rules for inheriting context:
///   - Budget     : kept until the user mentions a new one or says "any budget"
///   - Categories : kept until a new category is mentioned or none signals "all"
///   - Use-cases  : kept until overridden
///   - Keywords   : NOT inherited — keywords are specific to each turn
///   - Intent     : always from the current turn
///
/// Example:
///   Turn 1: "fitness gear"       → context: {Categories:[Fitness]}
///   Turn 2: "under R400"         → merge: {Categories:[Fitness], MaxBudget:400}
///   Turn 3: "show me electronics"→ merge: {Categories:[Electronics]} (replaces fitness)
/// </summary>
internal sealed class ConversationContext
{
    public decimal?     Budget     { get; init; }
    public decimal?     MinBudget  { get; init; }
    public List<string> Categories { get; init; } = [];
    public List<string> UseCases   { get; init; } = [];

    /// <summary>
    /// Merges this context with the current turn's query.
    /// The current turn always wins on conflicts; context fills in gaps.
    /// </summary>
    public ParsedQuery Merge(ParsedQuery current)
    {
        // Detect reset phrases that clear context
        var raw = current.Raw.ToLowerInvariant();
        var resetsContext = raw is "clear" or "reset" or "start over" or "new search"
            || raw.Contains("something else")
            || raw.Contains("never mind");

        if (resetsContext)
            return current;

        // Inherit budget if current turn doesn't mention one
        var mergedMax = current.MaxBudget ?? Budget;
        var mergedMin = current.MinBudget ?? MinBudget;

        // Use current categories if stated, otherwise inherit previous
        var mergedCategories = current.Categories.Any()
            ? current.Categories
            : Categories;

        // Merge use-cases (union — "gift for office use" builds up context)
        var mergedUseCases = current.UseCases
            .Union(UseCases)
            .Distinct()
            .ToList();

        // If everything is inherited and current has no signals at all,
        // treat it as a fresh General query to avoid stale responses
        var hasCurrentSignals = current.Keywords.Any()
                             || current.Categories.Any()
                             || current.MaxBudget.HasValue
                             || current.UseCases.Any();

        if (!hasCurrentSignals && !mergedMax.HasValue && !mergedCategories.Any())
            return current;

        // Rebuild the query with merged signals but current turn's intent and keywords
        return new ParsedQuery
        {
            Raw           = current.Raw,
            Intent        = current.Intent,
            MaxBudget     = mergedMax,
            MinBudget     = mergedMin,
            Categories    = mergedCategories,
            Keywords      = current.Keywords,   // keywords are turn-specific
            UseCases      = mergedUseCases,
            WantsTopRated = current.WantsTopRated,
            WantsNew      = current.WantsNew,
        };
    }

    /// <summary>Creates an updated context from the merged query for the next turn.</summary>
    public ConversationContext Update(ParsedQuery merged) => new()
    {
        Budget     = merged.MaxBudget,
        MinBudget  = merged.MinBudget,
        Categories = merged.Categories,
        UseCases   = merged.UseCases,
    };
}

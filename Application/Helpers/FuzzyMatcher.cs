namespace OnlineShoppingSystem.Application.Helpers;

/// <summary>
/// Scores how closely a piece of text matches a search query using a simple
/// but effective fuzzy algorithm:
///
///   1. Exact phrase match        → highest score
///   2. Every query word present  → high score (proportional to word count matched)
///   3. Partial / substring match → lower score (proportional to characters matched)
///
/// All comparisons are case-insensitive. Scores are normalised to [0, 1] so
/// callers can rank results consistently regardless of text length.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Returns a score in the range [0, 1] representing how closely
    /// <paramref name="text"/> matches <paramref name="query"/>.
    /// Returns 0 when there is no detectable similarity.
    /// </summary>
    public static double Score(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return 0;

        var normText  = text.ToLowerInvariant();
        var normQuery = query.Trim().ToLowerInvariant();

        // Tier 1 – exact phrase match anywhere in the text
        if (normText.Contains(normQuery))
            return 1.0;

        // Tier 2 – all individual query words appear in the text
        var words         = normQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords  = words.Count(w => normText.Contains(w));

        if (matchedWords == words.Length)
            return 0.8;

        // Tier 3 – some query words match
        if (matchedWords > 0)
            return 0.4 * matchedWords / words.Length;

        // Tier 4 – character-level overlap (catches typos / partial words)
        var charScore = CharacterOverlapScore(normText, normQuery);
        return charScore > 0 ? 0.2 * charScore : 0;
    }

    /// <summary>
    /// Measures what fraction of the query's characters appear, in order,
    /// as a subsequence of the text.
    /// </summary>
    private static double CharacterOverlapScore(string text, string query)
    {
        int matched  = 0;
        int textIdx  = 0;

        foreach (char c in query)
        {
            while (textIdx < text.Length && text[textIdx] != c)
                textIdx++;

            if (textIdx < text.Length)
            {
                matched++;
                textIdx++;
            }
        }

        return (double)matched / query.Length;
    }
}

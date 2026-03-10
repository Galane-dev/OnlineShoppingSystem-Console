using FluentAssertions;
using OnlineShoppingSystem.Application.Helpers;
using Xunit;

namespace OnlineShoppingSystem.Tests;

/// <summary>Tests for fuzzy search scoring and result ordering.</summary>
public class FuzzyMatcherTests
{
    // ── Score correctness ──────────────────────────────────────────────────────

    [Fact]
    public void Score_ExactPhraseMatch_ReturnsPerfectScore()
    {
        FuzzyMatcher.Score("Wireless Mouse", "Wireless Mouse").Should().Be(1.0);
    }

    [Fact]
    public void Score_SubstringMatch_ReturnsPerfectScore()
    {
        FuzzyMatcher.Score("Ergonomic Wireless Mouse", "wireless").Should().Be(1.0);
    }

    [Fact]
    public void Score_AllWordsPresent_ReturnsHighScore()
    {
        // Both words exist in the text but not as a contiguous phrase
        var score = FuzzyMatcher.Score("Portable Bluetooth Speaker", "bluetooth portable");

        score.Should().BeGreaterThan(0).And.BeLessThan(1.0);
    }

    [Fact]
    public void Score_NoMatch_ReturnsZero()
    {
        FuzzyMatcher.Score("Laptop Pro 15", "yoga mat").Should().Be(0);
    }

    [Fact]
    public void Score_EmptyQuery_ReturnsZero()
    {
        FuzzyMatcher.Score("Laptop Pro 15", "").Should().Be(0);
    }

    [Fact]
    public void Score_EmptyText_ReturnsZero()
    {
        FuzzyMatcher.Score("", "laptop").Should().Be(0);
    }

    // ── Ranking ────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_ExactMatch_RanksHigherThanPartialMatch()
    {
        var exact   = FuzzyMatcher.Score("Wireless Mouse", "wireless mouse");
        var partial = FuzzyMatcher.Score("Mouse Pad with wireless charging", "wireless mouse");

        exact.Should().BeGreaterThan(partial);
    }

    [Fact]
    public void Score_MoreWordsMatched_RanksHigherThanFewer()
    {
        var twoWords = FuzzyMatcher.Score("Bluetooth Speaker Portable", "bluetooth portable");
        var oneWord  = FuzzyMatcher.Score("Bluetooth Headphones", "bluetooth portable");

        twoWords.Should().BeGreaterThan(oneWord);
    }

    // ── Case insensitivity ─────────────────────────────────────────────────────

    [Fact]
    public void Score_IsCaseInsensitive()
    {
        var lower = FuzzyMatcher.Score("Wireless Mouse", "wireless mouse");
        var upper = FuzzyMatcher.Score("Wireless Mouse", "WIRELESS MOUSE");
        var mixed = FuzzyMatcher.Score("Wireless Mouse", "Wireless Mouse");

        lower.Should().Be(upper).And.Be(mixed);
    }
}

using Neba.Api.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Domain;

[UnitTest]
[Component("Domain")]
public sealed class SlugNormalizerTests
{
    [Theory(DisplayName = "Normalize converts titles into URL-safe slugs")]
    [InlineData("Hello World", "hello-world", TestDisplayName = "Simple title with a space")]
    [InlineData("HELLO WORLD", "hello-world", TestDisplayName = "Uppercase title is lowercased")]
    [InlineData("  Hello World  ", "hello-world", TestDisplayName = "Leading and trailing whitespace is trimmed")]
    [InlineData("Hello    World", "hello-world", TestDisplayName = "Multiple spaces collapse to a single hyphen")]
    [InlineData("Hello---World", "hello-world", TestDisplayName = "Multiple consecutive hyphens collapse to one")]
    [InlineData("Hello, World!", "hello-world", TestDisplayName = "Punctuation is replaced by a hyphen")]
    [InlineData("---Hello World---", "hello-world", TestDisplayName = "Leading punctuation is dropped, trailing hyphen trimmed")]
    [InlineData("Hello_World", "hello-world", TestDisplayName = "Underscore is treated as a separator")]
    [InlineData("Bowler #1 Award", "bowler-1-award", TestDisplayName = "Non-alphanumeric characters between words become single hyphens")]
    [InlineData("2026 Season Recap", "2026-season-recap", TestDisplayName = "Digits are preserved")]
    [InlineData("already-a-slug", "already-a-slug", TestDisplayName = "Already-normalized input is unchanged")]
    public void Normalize_ShouldProduceExpectedSlug_WhenGivenVariousInputs(string value, string expected)
    {
        // Act
        var result = SlugNormalizer.Normalize(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "Normalize returns an empty string when input is only whitespace")]
    public void Normalize_ShouldReturnEmptyString_WhenInputIsOnlyWhitespace()
    {
        // Act
        var result = SlugNormalizer.Normalize("   ");

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Normalize returns an empty string when input is empty")]
    public void Normalize_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = SlugNormalizer.Normalize(string.Empty);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Normalize returns an empty string when input has no alphanumeric characters")]
    public void Normalize_ShouldReturnEmptyString_WhenInputHasNoAlphanumericCharacters()
    {
        // Act
        var result = SlugNormalizer.Normalize("!!! ??? ---");

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Normalize never produces a leading hyphen")]
    public void Normalize_ShouldNotProduceLeadingHyphen_WhenInputStartsWithNonAlphanumericCharacters()
    {
        // Act
        var result = SlugNormalizer.Normalize("###Title");

        // Assert
        result.ShouldStartWith("title");
        result.ShouldNotStartWith("-");
    }

    [Fact(DisplayName = "Normalize never produces a trailing hyphen")]
    public void Normalize_ShouldNotProduceTrailingHyphen_WhenInputEndsWithNonAlphanumericCharacters()
    {
        // Act
        var result = SlugNormalizer.Normalize("Title###");

        // Assert
        result.ShouldNotEndWith("-");
    }
}

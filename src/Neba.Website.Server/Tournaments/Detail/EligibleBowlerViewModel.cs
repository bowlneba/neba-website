using System.Diagnostics.CodeAnalysis;

namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>A bowler on an eligibility list.</summary>
public sealed record EligibleBowlerViewModel
{
    /// <summary>The bowler's display name.</summary>
    public required string Name
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
            NameKey = NormalizeName(value);
        }
    }

    /// <summary>The bowler's USBC ID; null when none is on file.</summary>
    public string? UsbcId
    {
        get;
        init
        {
            field = value;
            UsbcKey = value is null ? string.Empty : NormalizeUsbcId(value);
        }
    }

    /// <summary>The name as searched: lower case, straight apostrophes.</summary>
    public string NameKey { get; private init; } = string.Empty;

    /// <summary>The USBC ID as searched: no dashes or spaces; empty when there is no ID.</summary>
    public string UsbcKey { get; private init; } = string.Empty;

    // Every word must be in the name, in any order. The ID is tried only when the term has a digit.
    internal bool Matches(string term)
    {
        var trimmed = term.Trim();

        if (trimmed.Length == 0)
        {
            return true;
        }

        var words = NormalizeName(trimmed).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (words.All(word => NameKey.Contains(word, StringComparison.Ordinal)))
        {
            return true;
        }

        return UsbcKey.Length > 0
            && trimmed.Any(char.IsAsciiDigit)
            && UsbcKey.Contains(NormalizeUsbcId(trimmed), StringComparison.OrdinalIgnoreCase);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "A lower-case search key, not a security comparison.")]
    private static string NormalizeName(string value)
        => value.ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'');

    private static string NormalizeUsbcId(string value)
        => string.Concat(value.Where(character => character != '-' && !char.IsWhiteSpace(character)));
}

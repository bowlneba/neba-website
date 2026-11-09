using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Neba.Website.Server.Documents;

/// <summary>Where a short anchor points in the rules document.</summary>
/// <param name="HeadingIdPrefix">Prefix of the heading ID, such as <c>rule-601-</c>.</param>
/// <param name="TitleKeyword">Text the heading must contain; used to find the rule again after a renumber.</param>
public sealed record AnchorAlias(string HeadingIdPrefix, string TitleKeyword);

/// <summary>A tournament type's rule section on the rules page.</summary>
/// <param name="Slug">Short anchor, such as <c>invitational</c>.</param>
/// <param name="RuleNumber">Rule number of the section heading.</param>
/// <param name="TitleKeyword">Text the heading must contain.</param>
public sealed record TournamentRulesAnchor(string Slug, int RuleNumber, string TitleKeyword)
{
    /// <summary>Link to the section on the full rules page.</summary>
    public string Href => TournamentRulesAnchors.RulesPath + "#" + Slug;

    /// <summary>Heading lookup for this section.</summary>
    public AnchorAlias Alias => new(
        "rule-" + RuleNumber.ToString(CultureInfo.InvariantCulture) + "-",
        TitleKeyword);
}

/// <summary>Short anchors for the tournament rules page, keyed by tournament type.</summary>
public static class TournamentRulesAnchors
{
    internal const string RulesPath = "/tournaments/rules";
    internal const string DocumentName = "tournament-rules";

    // Keyed by the tournament type display name the API returns.
    // Add an entry here when a new tournament type gets its own rule section (code change by design).
    private static readonly Dictionary<string, TournamentRulesAnchor> ByType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Singles"] = new("singles", 400, "Singles Tournament Format"),
            ["Doubles"] = new("doubles", 401, "Doubles Tournament Format"),
            ["Trios"] = new("trios", 402, "Trios Tournament Format"),
            ["Baker"] = new("baker", 403, "Baker Tournament Format"),
            ["Tournament of Champions"] = new("tournament-of-champions", 600, "Tournament of Champions"),
            ["Invitational"] = new("invitational", 601, "Invitational"),
            ["Masters"] = new("masters", 602, "Masters"),
            ["Non-Champions"] = new("non-champions", 603, "Non-Champion"),
        };

    /// <summary>Slug to alias, handed to <c>NebaDocument</c> so <c>#invitational</c> scrolls to the right heading.</summary>
    public static IReadOnlyDictionary<string, AnchorAlias> AnchorAliases { get; } =
        ByType.Values.ToDictionary(a => a.Slug, a => a.Alias, StringComparer.Ordinal);

    /// <summary>Finds the rule section for a tournament type.</summary>
    public static bool TryGet(string tournamentType, [NotNullWhen(true)] out TournamentRulesAnchor? anchor)
        => ByType.TryGetValue(tournamentType, out anchor);
}

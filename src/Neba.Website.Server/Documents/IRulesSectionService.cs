using ErrorOr;

namespace Neba.Website.Server.Documents;

/// <summary>One rule section, ready to show.</summary>
/// <param name="Html">HTML of the section as the API returned it, with links rewritten. This layer does not sanitize it; render it only because the API document is trusted.</param>
/// <param name="LastUpdated">When the rules document last changed, in the viewer's time zone.</param>
internal sealed record RulesSection(string Html, DateTimeOffset? LastUpdated);

internal interface IRulesSectionService
{
    /// <summary>
    /// Loads the rules document and cuts out the section <paramref name="anchor"/> points to.
    /// Returns <see cref="ErrorType.NotFound"/> when the document has no such section.
    /// </summary>
    Task<ErrorOr<RulesSection>> GetSectionAsync(TournamentRulesAnchor anchor, CancellationToken ct = default);
}
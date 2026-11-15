using ErrorOr;

using Neba.Api.Contracts.Documents;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;

namespace Neba.Website.Server.Documents;

internal sealed class RulesSectionService(
    ApiExecutor executor,
    IDocumentsApi documentsApi,
    IClientTimeZoneService clientTimeZoneService) : IRulesSectionService
{
    public async Task<ErrorOr<RulesSection>> GetSectionAsync(TournamentRulesAnchor anchor, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "Documents",
            "GetTournamentRules",
            token => documentsApi.GetDocumentAsync(TournamentRulesAnchors.DocumentName, token),
            ct);

        if (result.IsError)
        {
            // An API 404 for the document must not look like a missing section (also NotFound).
            return Error.Failure(
                "RulesSection.LoadFailed",
                $"The rules document could not be loaded: {result.FirstError.Description}");
        }

        var html = RulesSectionExtractor.Extract(result.Value.Html, anchor.Alias, TournamentRulesAnchors.RulesPath);

        if (html is null)
        {
            return Error.NotFound(
                "RulesSection.NotFound",
                $"No rule section found for '{anchor.Slug}'.");
        }

        var lastUpdated = await clientTimeZoneService.ToLocalOrNullAsync(result.Value.LastUpdated);

        return new RulesSection(html, lastUpdated);
    }

    public async Task<ErrorOr<RulesSection>> GetTeamRulesAsync(TeamRulesAnchor anchor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(anchor);

        var result = await executor.ExecuteAsync(
            "Documents",
            "GetTournamentRules",
            token => documentsApi.GetDocumentAsync(TournamentRulesAnchors.DocumentName, token),
            ct);

        if (result.IsError)
        {
            return Error.Failure(
                "RulesSection.LoadFailed",
                $"The rules document could not be loaded: {result.FirstError.Description}");
        }

        var pieces = new List<string>(anchor.SubRules.Count);

        for (var i = 0; i < anchor.SubRules.Count; i++)
        {
            // Only the first piece carries the list styles, so they are not repeated.
            var piece = RulesSectionExtractor.ExtractSubRule(
                result.Value.Html, anchor.SubRules[i], TournamentRulesAnchors.RulesPath, includeStyles: i == 0);

            if (piece is null)
            {
                return Error.NotFound(
                    "RulesSection.NotFound",
                    $"No rule {anchor.SubRules[i].HeadingPrefix} found.");
            }

            pieces.Add(piece);
        }

        var lastUpdated = await clientTimeZoneService.ToLocalOrNullAsync(result.Value.LastUpdated);

        return new RulesSection(string.Concat(pieces), lastUpdated);
    }
}
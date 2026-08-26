using System.Globalization;
using System.Net;

using Neba.Api.Email;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed class UnmappedBowlerStatsEmail(SeasonId seasonId, IReadOnlyCollection<int> unmappedLegacyBowlerIds)
{
    public string ToHtmlBody()
    {
        var rows = string.Concat(unmappedLegacyBowlerIds
            .Order()
            .Select(id => $"<tr><td>{WebUtility.HtmlEncode(id.ToString(CultureInfo.CurrentCulture))}</td></tr>"));

        var body = $"""
                    <p>Season <strong>{WebUtility.HtmlEncode(seasonId.ToString())}</strong> was regenerated, but the following legacy bowler ids have no matching website bowler (no <code>Bowler.LegacyId</code> match) and were excluded from the season's stats.</p>
                    <p>This usually means the <code>NewBowler</code>/<code>UpdateBowler</code> backdoor sync never ran for these bowlers. Re-triggering this season's stats generation again after they're linked will pick them up.</p>
                    <table><thead><tr><th>Legacy Bowler Id</th></tr></thead><tbody>{rows}</tbody></table>
                    """;

        return EmailLayout.Wrap(body);
    }
}
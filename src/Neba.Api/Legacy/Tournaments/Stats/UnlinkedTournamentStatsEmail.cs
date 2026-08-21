using System.Globalization;
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed class UnlinkedTournamentStatsEmail(int legacyTournamentId)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Legacy tournament id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> was reported for season stats generation, but no website tournament is linked to it (no <code>Tournament.LegacyId</code> match).</p>
                    <p>This usually means the <code>NewTournament</code> backdoor sync never ran or couldn't resolve a unique match. Season stats were not generated and will need to be re-triggered (re-firing the stats update again after the tournament is linked will pick it up).</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}
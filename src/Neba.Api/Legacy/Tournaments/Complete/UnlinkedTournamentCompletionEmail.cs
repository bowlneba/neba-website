using System.Globalization;
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal sealed class UnlinkedTournamentCompletionEmail(int legacyTournamentId)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Legacy tournament id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> was reported complete, but no website tournament is linked to it (no <code>Tournament.LegacyId</code> match).</p>
                    <p>This usually means the <code>NewTournament</code> backdoor sync never ran or couldn't resolve a unique match. Results were not synced and will need to be re-synced (re-triggering completion again after the tournament is linked will pick them up).</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}

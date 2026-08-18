using System.Globalization;
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Legacy.Tournaments.Complete;

internal sealed class UnsyncedBowlerResultSyncEmail(IReadOnlyCollection<int> legacyBowlerIds, int legacyTournamentId, bool isTeamTournament)
{
    public string ToHtmlBody()
    {
        var idRows = string.Concat(legacyBowlerIds
            .Select(id => $"<tr><td>{WebUtility.HtmlEncode(id.ToString(CultureInfo.CurrentCulture))}</td></tr>"));

        var teamNote = isTeamTournament
            ? "<p>This is a team tournament — an unmapped bowler is also excluded from their team's merged qualifying score, which may affect their teammates' computed <code>Place</code> if the team didn't advance.</p>"
            : "";

        var body = $"""
                    <p>Legacy tournament id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> completed with bowler(s) that have no matching website bowler (no <code>Bowler.LegacyId</code> match).</p>
                    {teamNote}
                    <p>This usually means the <code>NewBowler</code>/<code>UpdateBowler</code> backdoor sync never ran for them. Their results were not saved and will need to be re-synced.</p>
                    <table><thead><tr><th>Legacy Bowler Id</th></tr></thead><tbody>{idRows}</tbody></table>
                    """;

        return EmailLayout.Wrap(body);
    }
}

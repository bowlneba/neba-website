using System.Globalization;
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class UnknownLegacySeasonEmail(int legacySeasonId)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Legacy season id <strong>{WebUtility.HtmlEncode(legacySeasonId.ToString(CultureInfo.CurrentCulture))}</strong> was reported complete, but no matching legacy season record could be found.</p>
                    <p>This usually means the season was deleted or the id is otherwise invalid. Season completion and award assignment were skipped entirely.</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}

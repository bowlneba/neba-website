using System.Globalization;
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class UnmatchedSeasonEmail(int legacySeasonId, DateOnly startDate, DateOnly endDate)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Legacy season id <strong>{WebUtility.HtmlEncode(legacySeasonId.ToString(CultureInfo.CurrentCulture))}</strong> ({WebUtility.HtmlEncode(startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))} – {WebUtility.HtmlEncode(endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}) was reported complete, but no website season has a matching start/end date.</p>
                    <p>This usually means the website season for this date range hasn't been created yet, or its dates don't line up exactly. Season completion and award assignment were skipped entirely; re-firing completion again once a matching season exists will pick it up.</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}
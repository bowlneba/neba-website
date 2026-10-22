namespace Neba.Api.Legacy;

/// <summary>
/// Time zone helpers shared by the `/legacy` code. Deleted along with the rest of Legacy/ at Software sunset.
/// </summary>
internal static class LegacyTimeZones
{
    /// <summary>
    /// U.S. Eastern time: the zone of every date and time stored in the Software's database.
    /// </summary>
    public static TimeZoneInfo Eastern { get; } = FindEastern();

    private static TimeZoneInfo FindEastern()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            // fall through to the Windows id
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // An image without tzdata (or invariant globalization) must not stop the API from
            // starting: this is read at startup to register the weekly sync. A fixed UTC-5 offset
            // is off by an hour in summer, which is harmless for the sync's 8-day window.
            return TimeZoneInfo.CreateCustomTimeZone("Eastern (fixed)", TimeSpan.FromHours(-5), "Eastern (fixed)", "Eastern (fixed)");
        }
    }
}
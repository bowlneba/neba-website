namespace Neba.Api.Legacy;

/// <summary>
/// Time zone helpers shared by the `/legacy` code. Deleted along with the rest of Legacy/ at Software sunset.
/// </summary>
internal static class LegacyTimeZones
{
    /// <summary>
    /// U.S. Eastern time: the zone of every date and time stored in the Software's database.
    /// </summary>
    public static TimeZoneInfo Eastern => EasternTimeZone.Instance;
}
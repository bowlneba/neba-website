namespace Neba.Api;

/// <summary>
/// U.S. Eastern time, the zone of every Software date and time and of the Series designations. Shared by
/// <c>Features</c> and <c>/legacy</c>, so neither copies the lookup.
/// </summary>
internal static class EasternTimeZone
{
    /// <summary>The Eastern zone, or a fixed UTC-5 zone on an image with no time zone data.</summary>
    public static TimeZoneInfo Instance { get; } = Find();

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "America/New_York", "Eastern Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try the next id
            }
        }

        // An image without tzdata (or invariant globalization) must not stop the API from starting: the weekly
        // sync reads this at startup. A fixed UTC-5 offset is off by an hour in summer.
        return TimeZoneInfo.CreateCustomTimeZone("Eastern (fixed)", TimeSpan.FromHours(-5), "Eastern (fixed)", "Eastern (fixed)");
    }
}
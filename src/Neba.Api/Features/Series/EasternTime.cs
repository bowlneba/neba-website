namespace Neba.Api.Features.Series;

// Squad times are stored in UTC; bowlers and the Series designation use Eastern wall-clock time. Non-legacy copy of
// LegacyTimeZones.Eastern, since Features must not depend on /legacy.
internal static class EasternTime
{
    private static readonly TimeZoneInfo Zone = Find();

    public static (DateOnly Date, TimeOnly Time) ToLocal(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, Zone).DateTime;

        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }

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

        return TimeZoneInfo.CreateCustomTimeZone("Eastern (fixed)", TimeSpan.FromHours(-5), "Eastern (fixed)", "Eastern (fixed)");
    }
}
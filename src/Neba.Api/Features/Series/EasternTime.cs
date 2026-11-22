namespace Neba.Api.Features.Series;

// Squad times are stored in UTC; bowlers and the Series designation use Eastern wall-clock time.
internal static class EasternTime
{
    public static (DateOnly Date, TimeOnly Time) ToLocal(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, EasternTimeZone.Instance).DateTime;

        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }
}
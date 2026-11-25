namespace Neba.Api.Features.Series;

/// <summary>
/// Orders a Stage's field entries: placed bowlers first by place, then by Bowler of the Year points (high to low),
/// then by name and Bowler id. Shared by the admin and public advancement reads so both show the same order.
/// </summary>
internal static class FieldEntryOrder
{
    public static IOrderedEnumerable<T> InFieldOrder<T, TId>(
        this IEnumerable<T> entries,
        Func<T, int?> place,
        Func<T, int?> backfillPoints,
        Func<T, string> lastName,
        Func<T, string> firstName,
        Func<T, TId> bowlerId)
        where TId : IComparable<TId>
    {
        return entries
            .OrderBy(e => place(e) ?? int.MaxValue)
            .ThenByDescending(e => backfillPoints(e) ?? 0)
            .ThenBy(e => lastName(e), StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => firstName(e), StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => bowlerId(e));
    }
}

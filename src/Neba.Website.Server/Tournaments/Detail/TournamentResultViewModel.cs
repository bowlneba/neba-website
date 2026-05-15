using System.Globalization;

namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// Per-bowler result row for display in the results table.
/// </summary>
public sealed record TournamentResultViewModel
{
    private static readonly CultureInfo CurrencyCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Display name of the bowler.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Finishing place; null when place was not recorded.
    /// </summary>
    public int? Place { get; init; }

    /// <summary>
    /// Prize money awarded in USD.
    /// </summary>
    public decimal PrizeMoney { get; init; }

    /// <summary>
    /// Season points awarded.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// Name of the side cut the bowler competed in; null for the main cut.
    /// </summary>
    public string? SideCutName { get; init; }

    /// <summary>
    /// CSS hex color for the side cut indicator; null for the main cut.
    /// </summary>
    public string? SideCutIndicator { get; init; }

    /// <summary>
    /// Place formatted for display; em dash when no place was recorded.
    /// </summary>
    public string FormattedPlace =>
        Place.HasValue
            ? Place.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "—";

    /// <summary>
    /// Prize money formatted as currency.
    /// </summary>
    public string FormattedPrizeMoney =>
        PrizeMoney.ToString("C0", CurrencyCulture);
}
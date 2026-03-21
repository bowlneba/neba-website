using Ardalis.SmartEnum;

namespace Neba.Domain.Awards;

/// <summary>
/// The type of season award.
/// </summary>
/// <remarks>
/// Name values are stored as discriminator column values in the <c>season_awards</c> table.
/// Use explicit string literals — NOT <see langword="nameof()"/> — so DB values are stable against C# renames.
/// </remarks>
public sealed class SeasonAwardType 
    : SmartEnum<SeasonAwardType>
{
    /// <summary>
    /// Recognizes overall performance across Stat-Eligible Tournaments, awarded per category.
    /// </summary>
    public static readonly SeasonAwardType BowlerOfTheYear = new("BowlerOfTheYear", 1);

    /// <summary>
    /// Recognizes the highest pinfall average across Stat-Eligible Tournaments.
    /// </summary>
    public static readonly SeasonAwardType HighAverage = new("HighAverage", 2);

    /// <summary>
    /// Recognizes the highest 5-game qualifying block score in any Stat-Eligible Tournament.
    /// </summary>
    public static readonly SeasonAwardType HighBlock = new("HighBlock", 3);

    private SeasonAwardType(string name, int value) 
        : base(name, value) { }
}

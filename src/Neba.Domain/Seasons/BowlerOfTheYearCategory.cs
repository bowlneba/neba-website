using Ardalis.SmartEnum;

namespace Neba.Domain.Seasons;

/// <summary>
/// The competitive category under which a Bowler of the Year award is given.
/// Age eligibility is evaluated as of each tournament date during the season.
/// </summary>
public sealed class BowlerOfTheYearCategory
    : SmartEnum<BowlerOfTheYearCategory>
{
    /// <summary>
    /// All eligible bowlers.
    /// </summary>
    public static readonly BowlerOfTheYearCategory Open = new("Open", 1);

    /// <summary>
    /// Female bowlers.
    /// </summary>
    public static readonly BowlerOfTheYearCategory Woman = new("Woman", 2);

    /// <summary>
    /// Bowlers age 50 or older.
    /// </summary>
    public static readonly BowlerOfTheYearCategory Senior = new("Senior", 50);

    /// <summary>
    /// Bowlers age 60 or older.
    /// </summary>
    public static readonly BowlerOfTheYearCategory SuperSenior = new("Super Senior", 60);

    /// <summary>
    /// Bowlers paying a New Member membership in the current season.
    /// </summary>
    public static readonly BowlerOfTheYearCategory Rookie = new("Rookie", 3);

    /// <summary>
    /// Bowlers under age 18.
    /// </summary>
    public static readonly BowlerOfTheYearCategory Youth = new("Youth", 18);

    private BowlerOfTheYearCategory(string name, int value)
        : base(name, value) { }
}
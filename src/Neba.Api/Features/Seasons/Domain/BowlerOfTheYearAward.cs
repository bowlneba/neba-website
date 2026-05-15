using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Seasons.Domain;

/// <summary>
/// Recognizes overall performance across Stat-Eligible Tournaments during the Season.
/// A separate record exists for each <see cref="BowlerOfTheYearCategory"/> a bowler wins within a season.
/// </summary>
public sealed class BowlerOfTheYearAward
{
    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public required SeasonAwardId Id { get; init; }

    /// <summary>
    /// The bowler receiving the award.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The category in which the award is given.
    /// Age eligibility is evaluated as of each tournament date during the season.
    /// </summary>
    public required BowlerOfTheYearCategory Category { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;

    internal static ErrorOr<BowlerOfTheYearAward> CreateOpen(BowlerId bowlerId)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.Open };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateWoman(BowlerId bowlerId, Gender gender)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        if (gender != Gender.Female)
        {
            return BowlerOfTheYearAwardErrors.NotFemale;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.Woman };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateSenior(BowlerId bowlerId, int age)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        if (age < 50)
        {
            return BowlerOfTheYearAwardErrors.InsufficientAgeForSenior;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.Senior };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateSuperSenior(BowlerId bowlerId, int age)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        if (age < 60)
        {
            return BowlerOfTheYearAwardErrors.InsufficientAgeForSuperSenior;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.SuperSenior };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateRookie(BowlerId bowlerId, bool isRookie)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        if (!isRookie)
        {
            return BowlerOfTheYearAwardErrors.NotARookie;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.Rookie };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateYouth(BowlerId bowlerId, int age)
    {
        if (bowlerId.Equals(default))
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        if (age >= 18)
        {
            return BowlerOfTheYearAwardErrors.AgeExceedsYouthLimit;
        }

        var id = SeasonAwardId.New();
        return new BowlerOfTheYearAward { Id = id, BowlerId = bowlerId, Category = BowlerOfTheYearCategory.Youth };
    }
}
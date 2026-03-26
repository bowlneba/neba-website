using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Seasons;

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
        return bowlerId == BowlerId.Empty
            ? BowlerOfTheYearAwardErrors.BowlerIdRequired
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.Open
        };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateWoman(BowlerId bowlerId, Gender gender)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        return gender != Gender.Female
            ? BowlerOfTheYearAwardErrors.NotFemale
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.Woman
        };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateSenior(BowlerId bowlerId, int age)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        return age < 50
            ? BowlerOfTheYearAwardErrors.InsufficientAgeForSenior
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.Senior
        };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateSuperSenior(BowlerId bowlerId, int age)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        return age < 60
            ? BowlerOfTheYearAwardErrors.InsufficientAgeForSuperSenior
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.SuperSenior
        };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateRookie(BowlerId bowlerId, bool isRookie)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        return !isRookie
            ? BowlerOfTheYearAwardErrors.NotARookie
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.Rookie
        };
    }

    internal static ErrorOr<BowlerOfTheYearAward> CreateYouth(BowlerId bowlerId, int age)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return BowlerOfTheYearAwardErrors.BowlerIdRequired;
        }

        return age >= 18
            ? BowlerOfTheYearAwardErrors.AgeExceedsYouthLimit
            : new BowlerOfTheYearAward
        {
            Id = SeasonAwardId.New(),
            BowlerId = bowlerId,
            Category = BowlerOfTheYearCategory.Youth
        };
    }
}

internal static class BowlerOfTheYearAwardErrors
{
    public static readonly Error BowlerIdRequired = Error.Validation(
        code: "BowlerOfTheYearAward.BowlerIdRequired",
        description: "Bowler ID is required.");

    public static readonly Error NotFemale = Error.Validation(
        code: "BowlerOfTheYearAward.NotFemale",
        description: "The Woman category requires a female bowler.");

    public static readonly Error InsufficientAgeForSenior = Error.Validation(
        code: "BowlerOfTheYearAward.InsufficientAgeForSenior",
        description: "The Senior category requires the bowler to be at least 50 years old.");

    public static readonly Error InsufficientAgeForSuperSenior = Error.Validation(
        code: "BowlerOfTheYearAward.InsufficientAgeForSuperSenior",
        description: "The Super Senior category requires the bowler to be at least 60 years old.");

    public static readonly Error NotARookie = Error.Validation(
        code: "BowlerOfTheYearAward.NotARookie",
        description: "The Rookie category requires the bowler to hold a New Member membership in the current season.");

    public static readonly Error AgeExceedsYouthLimit = Error.Validation(
        code: "BowlerOfTheYearAward.AgeExceedsYouthLimit",
        description: "The Youth category requires the bowler to be under 18 years old.");
}
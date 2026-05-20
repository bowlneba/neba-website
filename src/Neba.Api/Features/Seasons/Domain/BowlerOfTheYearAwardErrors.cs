using ErrorOr;

namespace Neba.Api.Features.Seasons.Domain;

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
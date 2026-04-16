using Neba.Application.Bowlers;
using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A read model containing all performance metrics, classification flags, award points, and financial totals
/// for a single bowler within a single Season.
/// </summary>
public sealed record BowlerSeasonStatsDto
{
    /// <summary>
    /// The unique identifier of the Bowler to whom these statistics belong.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name, split into structured name parts for API and UI consumers.
    /// </summary>
    public required BowlerNameDto BowlerName { get; init; }

    /// <summary>
    /// Indicates whether the bowler held active NEBA membership during this Season. Membership status affects
    /// award eligibility and whether participation counts toward official statistics.
    /// </summary>
    public required bool IsMember { get; init; }

    /// <summary>
    /// Indicates whether the bowler is classified as a Rookie for this Season. A Rookie is a bowler competing in
    /// their first season as a paid NEBA member. A bowler may participate as a non-member prior to their rookie
    /// season; the rookie classification begins with the first paid membership.
    /// </summary>
    public required bool IsRookie { get; init; }

    /// <summary>
    /// Indicates whether the bowler is classified as a Senior for this Season. Seniors are bowlers who are age 50 or older.
    /// </summary>
    public required bool IsSenior { get; init; }

    /// <summary>
    /// Indicates whether the bowler is classified as a Super Senior for this Season. Super Seniors are bowlers who are
    /// age 60 or older. Super Senior is not mutually exclusive with Senior - a Super Senior satisfies both
    /// classifications and is eligible for both award tracks.
    /// </summary>
    public required bool IsSuperSenior { get; init; }

    /// <summary>
    /// Indicates whether the bowler competes under the Women's classification, making them eligible for
    /// Woman of the Year standings.
    /// </summary>
    public required bool IsWoman { get; init; }

    /// <summary>
    /// Indicates whether the bowler is classified as a Youth for this Season. NEBA defines Youth as bowlers
    /// under the age of 18.
    /// </summary>
    public required bool IsYouth { get; init; }

    /// <summary>
    /// The number of distinct tournaments the bowler participated in during the Season that count toward official
    /// season statistics and award calculations.
    /// </summary>
    public required int EligibleTournaments { get; init; }

    /// <summary>
    /// The total number of distinct tournaments the bowler participated in during the Season, including those
    /// not eligible toward official statistics or award calculations.
    /// </summary>
    public required int TotalTournaments { get; init; }

    /// <summary>
    /// The number of tournament entries during the Season that count toward official season statistics and award
    /// calculations.
    /// </summary>
    public required int EligibleEntries { get; init; }

    /// <summary>
    /// The total number of tournament entries during the Season, including entries in tournaments not eligible
    /// toward official statistics or award calculations.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// The number of times the bowler achieved a qualifying score high enough to earn prize money across all
    /// tournaments in the Season. Cashing is distinct from advancing to the Finals.
    /// </summary>
    public required int Cashes { get; init; }

    /// <summary>
    /// The number of tournaments in which the bowler advanced to the Finals (match play round) during the Season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// The highest single game the bowler bowled during the qualifying portion of any tournament in the Season.
    /// Does not include match play games.
    /// </summary>
    public required int QualifyingHighGame { get; init; }

    /// <summary>
    /// The highest score the bowler achieved across a 5-game qualifying block in the Season. Only tournaments
    /// with 5 or more qualifying games contribute. For tournaments with more than 5 qualifying games, each
    /// sequential group of 5 games is evaluated independently.
    /// </summary>
    public required int HighBlock { get; init; }

    /// <summary>
    /// The total number of head-to-head match play victories recorded across all Finals appearances in the Season.
    /// </summary>
    public required int MatchPlayWins { get; init; }

    /// <summary>
    /// The total number of head-to-head match play defeats recorded across all Finals appearances in the Season.
    /// </summary>
    public required int MatchPlayLosses { get; init; }

    /// <summary>
    /// The total number of individual match play games bowled across all Finals appearances in the Season.
    /// </summary>
    public required int MatchPlayGames { get; init; }

    /// <summary>
    /// The cumulative total pins knocked down across all match play games in the Season.
    /// </summary>
    public required int MatchPlayPinfall { get; init; }

    /// <summary>
    /// The highest single game bowled during match play in any tournament in the Season.
    /// </summary>
    public required int MatchPlayHighGame { get; init; }

    /// <summary>
    /// The cumulative count of all games bowled across all tournaments in the Season, spanning both qualifying
    /// and match play.
    /// </summary>
    public required int TotalGames { get; init; }

    /// <summary>
    /// The cumulative total pins knocked down across all games in the Season.
    /// </summary>
    public required int TotalPinfall { get; init; }

    /// <summary>
    /// The bowler's average performance relative to the competitive field across tournaments in the Season.
    /// Calculated as: (bowler's total qualifying pinfall / bowler's qualifying games) minus (all qualifying
    /// pinfall / all qualifying games) for each tournament entered, expressed as a signed decimal where a
    /// positive value indicates above-field performance.
    /// </summary>
    public required decimal FieldAverage { get; init; }

    /// <summary>
    /// The best finishing position the bowler achieved in any single tournament during the Season (e.g., 1 for
    /// first place). Null if the bowler did not receive a finishing position in any tournament.
    /// </summary>
    public required int? HighFinish { get; init; }

    /// <summary>
    /// The mean finishing position across all tournaments in which the bowler received a finishing position
    /// during the Season. Null if no finishing positions were recorded.
    /// </summary>
    public required decimal? AverageFinish { get; init; }

    /// <summary>
    /// Points accumulated toward the Bowler of the Year (Open) award for the Season.
    /// </summary>
    public required int BowlerOfTheYearPoints { get; init; }

    /// <summary>
    /// Points accumulated toward the Senior of the Year award for the Season. Applicable when
    /// <see cref="IsSenior"/> is true.
    /// </summary>
    public required int SeniorOfTheYearPoints { get; init; }

    /// <summary>
    /// Points accumulated toward the Super Senior of the Year award for the Season. Applicable when
    /// <see cref="IsSuperSenior"/> is true.
    /// </summary>
    public required int SuperSeniorOfTheYearPoints { get; init; }

    /// <summary>
    /// Points accumulated toward the Woman of the Year award for the Season. Applicable when
    /// <see cref="IsWoman"/> is true.
    /// </summary>
    public required int WomanOfTheYearPoints { get; init; }

    /// <summary>
    /// Points accumulated toward the Youth of the Year award for the Season. Applicable when
    /// <see cref="IsYouth"/> is true.
    /// </summary>
    public required int YouthOfTheYearPoints { get; init; }

    /// <summary>
    /// The total cash prize money earned by the bowler across all tournaments in the Season, excluding
    /// Cup earnings.
    /// </summary>
    public required decimal TournamentWinnings { get; init; }

    /// <summary>
    /// The total prize money earned by the bowler through Cup events during the Season. A Cup is a competition
    /// in which points earned across a set of pre-designated tournaments are accumulated, and prize money is paid
    /// to the top point earners at the conclusion of those tournaments.
    /// </summary>
    public required decimal CupEarnings { get; init; }

    /// <summary>
    /// The total Credits earned by the bowler during the Season. A Credit is a non-cash award applied as a
    /// discount toward a future tournament entry fee. Credits may be earned by achieving a qualifying score above
    /// a defined threshold without advancing to Finals, or may be granted by the Tournament Director for any
    /// reason at their discretion.
    /// </summary>
    public required decimal Credits { get; init; }

    /// <summary>
    /// The UTC timestamp of the most recent update to these statistics.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; init; }
}
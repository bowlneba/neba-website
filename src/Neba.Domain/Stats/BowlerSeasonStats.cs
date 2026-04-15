using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;

namespace Neba.Domain.Stats;

/// <summary>
/// Represents a bowler's season statistics, including performance metrics and points for various awards. This class serves as an aggregate root for accessing and managing a bowler's season stats in the application.
/// </summary>
public sealed class BowlerSeasonStats
    : AggregateRoot
{
    /// <summary>
    /// Gets the unique identifier for the season these statistics pertain to. This is a required property that links the stats to a specific season in the application.
    /// </summary>
    public required SeasonId SeasonId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the bowler these statistics pertain to. This is a required property that links the stats to a specific bowler in the application.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler was a member during the season. This is a required property that helps determine eligibility for certain awards and rankings in the application.
    /// </summary>
    public bool IsMember { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler is classified as a rookie for the season. This is a required property that helps determine eligibility for rookie-specific awards and rankings in the application.
    /// </summary>
    public bool IsRookie { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler is classified as a senior for the season. This is a required property that helps determine eligibility for senior-specific awards and rankings in the application.
    /// </summary>
    public bool IsSenior { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler is classified as a super senior for the season. This is a required property that helps determine eligibility for super senior-specific awards and rankings in the application.
    /// </summary>
    public bool IsSuperSenior { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler is classified as a woman for the season. This is a required property that helps determine eligibility for woman-specific awards and rankings in the application.
    /// </summary>
    public bool IsWoman { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bowler is classified as a youth for the season. This is a required property that helps determine eligibility for youth-specific awards and rankings in the application.
    /// </summary>
    public bool IsYouth { get; init; }

    /// <summary>
    /// Gets the number of tournaments the bowler participated in during the season that were eligible towards statistics and awards. This is a required property that contributes to various performance metrics and points calculations in the application.
    /// </summary>
    public int Tournaments { get; init; }

    /// <summary>
    /// Gets the total number of tournaments the bowler participated in during the season, including those that may not have been eligible towards statistics and awards. This is a required property that provides additional context about the bowler's activity during the season in the application.
    /// </summary>
    public int TotalTournaments { get; init; }

    /// <summary>
    /// Gets the number of tournament entries the bowler had during the season that were eligible towards statistics and awards. This is a required property that contributes to various performance metrics and points calculations in the application.
    /// </summary>
    public int Entries { get; init; }

    /// <summary>
    /// Gets the total number of tournament entries the bowler had during the season, including those that may not have been eligible towards statistics and awards. This is a required property that provides additional context about the bowler's activity during the season in the application.
    /// </summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// Gets the number of times the bowler cashed in a tournament during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of consistent performance across tournaments.
    /// </summary>
    public int Cashes { get; init; }

    /// <summary>
    /// Gets the number of times the bowler made the finals in a tournament during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of high-level performance across tournaments.
    /// </summary>
    public int Finals { get; init; }

    /// <summary>
    /// Gets the highest qualifying game the bowler achieved during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's peak performance during the season.
    /// </summary>
    public int QualifyingHighGame { get; init; }

    /// <summary>
    /// Gets the highest block of games the bowler achieved during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's consistency and endurance during the season.
    /// </summary>
    public int HighBlock { get; init; }

    /// <summary>
    /// Gets the number of match play wins the bowler achieved during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's head-to-head performance during the season.
    /// </summary>
    public int MatchPlayWins { get; init; }

    /// <summary>
    /// Gets the number of match play losses the bowler had during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's head-to-head performance during the season.
    /// </summary>
    public int MatchPlayLosses { get; init; }

    /// <summary>
    /// Gets the number of match play games the bowler participated in during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's head-to-head performance during the season.
    /// </summary>
    public int MatchPlayGames { get; init; }

    /// <summary>
    /// Gets the total pinfall the bowler achieved in match play games during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's head-to-head performance during the season.
    /// </summary>
    public int MatchPlayPinfall { get; init; }

    /// <summary>
    /// Gets the highest game the bowler achieved in match play during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's peak head-to-head performance during the season.
    /// </summary>
    public int MatchPlayHighGame { get; init; }

    /// <summary>
    /// Gets the total number of games the bowler played during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's overall activity and experience during the season.
    /// </summary>
    public int TotalGames { get; init; }

    /// <summary>
    /// Gets the total pinfall the bowler achieved during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's overall scoring performance during the season.
    /// </summary>
    public int TotalPinfall { get; init; }

    /// <summary>
    /// Gets the differential of the bowler's average compared to the field in the tournaments in which they bowled. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's performance relative to their peers during the season.
    /// </summary>
    public decimal FieldAverage { get; init; }

    /// <summary>
    /// Gets the highest finish position the bowler achieved in a tournament during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's best performance in terms of final standings during the season.
    /// </summary>
    public int? HighFinish { get; init; }

    /// <summary>
    /// Gets the average finish position of the bowler in tournaments during the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's overall consistency and performance in terms of final standings during the season.
    /// </summary>
    public decimal? AverageFinish { get; init; }

    /// <summary>
    /// Gets the points the bowler earned towards the Bowler of the Year award for the season. This is a required property that contributes to the bowler's overall ranking and eligibility for the Bowler of the Year award in the application.
    /// </summary>
    public int BowlerOfTheYearPoints { get; init; }

    /// <summary>
    /// Gets the points the bowler earned towards the Senior of the Year award for the season. This is a required property that contributes to the bowler's overall ranking and eligibility for the Senior of the Year award in the application.
    /// </summary>
    public int SeniorOfTheYearPoints { get; init; }

    /// <summary>
    /// Gets the points the bowler earned towards the Super Senior of the Year award for the season. This is a required property that contributes to the bowler's overall ranking and eligibility for the Super Senior of the Year award in the application.
    /// </summary>
    public int SuperSeniorOfTheYearPoints { get; init; }

    /// <summary>
    /// Gets the points the bowler earned towards the Woman of the Year award for the season. This is a required property that contributes to the bowler's overall ranking and eligibility for the Woman of the Year award in the application.
    /// </summary>
    public int WomanOfTheYearPoints { get; init; }

    /// <summary>
    /// Gets the points the bowler earned towards the Youth of the Year award for the season. This is a required property that contributes to the bowler's overall ranking and eligibility for the Youth of the Year award in the application.
    /// </summary>
    public int YouthOfTheYearPoints { get; init; }

    /// <summary>
    /// Gets the total tournament winnings for the bowler in the given season
    /// </summary>
    public decimal TournamentWinnings { get; init; }

    /// <summary>
    /// Gets the total winnings during Cup events during the course of the season. This is a required property that contributes to various performance metrics and points calculations in the application, and is often used as an indicator of a bowler's success in high-profile events during the season.
    /// </summary>
    public decimal CupEarnings { get; init; }

    /// <summary>
    /// Gets the total amount of Credits earned throughout the course of the season
    /// </summary>
    public decimal Credits { get; init; }

    /// <summary>
    /// Gets the date and time when the bowler's season statistics were last updated. This is a required property that helps track the freshness of the data and can be used for caching and display purposes in the application.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; init; }
}
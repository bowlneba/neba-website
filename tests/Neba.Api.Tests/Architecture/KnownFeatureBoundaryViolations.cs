namespace Neba.Api.Tests.Architecture;

/// <summary>
/// Cross-feature domain dependencies that existed when the architecture tests were added.
/// The list may only shrink: <see cref="FeatureBoundaryTests"/> fails on any dependency not listed here,
/// and on any entry listed here that no longer exists, so remove entries as you fix them.
/// </summary>
internal static class KnownFeatureBoundaryViolations
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "Neba.Api.Features.Bowlers.Domain.Bowler -> Neba.Api.Features.Stats.Domain.BowlerSeasonStats",
        "Neba.Api.Features.HallOfFame.Domain.HallOfFameInduction -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.HallOfFame.Domain.HallOfFameInduction -> Neba.Api.Features.Storage.Domain.StoredFile",
        "Neba.Api.Features.News.Domain.Article -> Neba.Api.Features.Storage.Domain.StoredFile",
        "Neba.Api.Features.News.Domain.Article -> Neba.Api.Features.Tournaments.Domain.Tournament",
        "Neba.Api.Features.News.Domain.ArticleAttachment -> Neba.Api.Features.Storage.Domain.StoredFile",
        "Neba.Api.Features.Seasons.Domain.BowlerOfTheYearAward -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Seasons.Domain.BowlerOfTheYearAward -> Neba.Api.Features.Bowlers.Domain.Gender",
        "Neba.Api.Features.Seasons.Domain.HighAverageAward -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Seasons.Domain.HighBlockAward -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Seasons.Domain.Season -> Neba.Api.Features.Bowlers.Domain.Gender",
        "Neba.Api.Features.Seasons.Domain.Season -> Neba.Api.Features.Stats.Domain.BowlerSeasonStats",
        "Neba.Api.Features.Seasons.Domain.Season -> Neba.Api.Features.Tournaments.Domain.Tournament",
        "Neba.Api.Features.Sponsors.Domain.Sponsor -> Neba.Api.Features.Storage.Domain.StoredFile",
        "Neba.Api.Features.Sponsors.Domain.Sponsor -> Neba.Api.Features.Tournaments.Domain.TournamentSponsor",
        "Neba.Api.Features.Stats.Domain.BowlerSeasonStats -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Stats.Domain.BowlerSeasonStats -> Neba.Api.Features.Seasons.Domain.Season",
        "Neba.Api.Features.Tournaments.Domain.SideCut -> Neba.Api.Features.Bowlers.Domain.Gender",
        "Neba.Api.Features.Tournaments.Domain.SideCutCriteria -> Neba.Api.Features.Bowlers.Domain.Gender",
        "Neba.Api.Features.Tournaments.Domain.SideCutCriteriaGroup -> Neba.Api.Features.Bowlers.Domain.Gender",
        "Neba.Api.Features.Tournaments.Domain.SquadScore -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Tournaments.Domain.Tournament -> Neba.Api.Features.BowlingCenters.Domain.BowlingCenter",
        "Neba.Api.Features.Tournaments.Domain.Tournament -> Neba.Api.Features.BowlingCenters.Domain.CertificationNumber",
        "Neba.Api.Features.Tournaments.Domain.Tournament -> Neba.Api.Features.News.Domain.Article",
        "Neba.Api.Features.Tournaments.Domain.Tournament -> Neba.Api.Features.Seasons.Domain.Season",
        "Neba.Api.Features.Tournaments.Domain.Tournament -> Neba.Api.Features.Storage.Domain.StoredFile",
        "Neba.Api.Features.Tournaments.Domain.TournamentErrors -> Neba.Api.Features.BowlingCenters.Domain.CertificationNumber",
        "Neba.Api.Features.Tournaments.Domain.TournamentResult -> Neba.Api.Features.Bowlers.Domain.Bowler",
        "Neba.Api.Features.Tournaments.Domain.TournamentSponsor -> Neba.Api.Features.Sponsors.Domain.Sponsor",
        "Neba.Api.Features.Tournaments.Domain.TournamentValidationService -> Neba.Api.Features.Seasons.Domain.Season",
        "Neba.Api.Features.Tournaments.Domain.TournamentValidationService -> Neba.Api.Features.Seasons.Domain.SeasonErrors",
    };
}

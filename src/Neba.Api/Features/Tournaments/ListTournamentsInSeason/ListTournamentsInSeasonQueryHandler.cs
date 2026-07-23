using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.ListSeasons;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService,
    TimeProvider timeProvider)
    : IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>
{
    private readonly IQueryable<Tournament> _tournaments =
        appDbContext.Tournaments.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions =
        appDbContext.HistoricalTournamentChampions.AsNoTracking();

    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> HandleAsync(ListTournamentsInSeasonQuery query, CancellationToken cancellationToken)
    {
        var rows = await _tournaments
            .Where(tournament => tournament.SeasonId == query.SeasonId)
            .Select(tournament => new
            {
                DbId = EF.Property<int>(tournament, ShadowIdConfiguration.DefaultPropertyName),
                tournament.Id,
                tournament.Name,
                Season = new SeasonDto
                {
                    Id = tournament.Season.Id,
                    Description = tournament.Season.Description,
                    StartDate = tournament.Season.StartDate,
                    EndDate = tournament.Season.EndDate
                },
                tournament.StartDate,
                tournament.EndDate,
                tournament.StatsEligible,
                TournamentType = tournament.TournamentType.Name,
                BowlingCenter = tournament.BowlingCenter == null
                    ? null
                    : new SeasonTournamentBowlingCenterDto
                    {
                        Name = tournament.BowlingCenter.Name,
                        City = tournament.BowlingCenter.Address.City,
                        State = tournament.BowlingCenter.Address.Region
                    },
                Sponsors = tournament.Sponsors
                    .Select(tournamentSponsor => tournamentSponsor.Sponsor)
                    .Select(s => new
                    {
                        s.Name,
                        s.Slug,
                        LogoContainer = s.Logo != null ? s.Logo.Container : null,
                        LogoPath = s.Logo != null ? s.Logo.Path : null
                    }).ToList(),
                AddedMoney = tournament.Sponsors.Sum(ts => ts.SponsorshipAmount),
                PatternLengthCategory = tournament.PatternLengthCategory == null
                    ? null
                    : tournament.PatternLengthCategory.Name,
                PatternRatioCategory = tournament.PatternRatioCategory == null
                    ? null
                    : tournament.PatternRatioCategory.Name,
                tournament.OilPatternRevealDateTime,
                tournament.EntryFee,
                RegistrationUrl = tournament.ExternalRegistrationUrl,
                TournamentLogoContainer = tournament.Logo != null
                    ? tournament.Logo.Container
                    : null,
                TournamentLogoPath = tournament.Logo != null
                    ? tournament.Logo.Path
                    : null,
                Reservations = 999, // need to replace once actual column exists
                OilPatterns = tournament.OilPatterns.Select(top => new
                {
                    top.OilPattern.Name,
                    top.OilPattern.Length,
                    top.OilPattern.Volume,
                    top.OilPattern.LeftRatio,
                    top.OilPattern.RightRatio,
                    top.OilPattern.KegelId,
                    top.TournamentRounds
                }).ToList()
            }).ToListAsync(cancellationToken);

        // We will need to do a separate query to get the champions from tournaments that we have full stats (2026+)
        var dbIds = rows.ConvertAll(r => r.DbId);

        var historicalWinners = await _historicalTournamentChampions
            .Where(tc => dbIds.Contains(tc.TournamentId))
            .Select(tc => new { tc.TournamentId, tc.Bowler.Name })
            .ToListAsync(cancellationToken);

        Dictionary<int, IReadOnlyCollection<Name>> historicalWinnersByTournamentDbId =
            historicalWinners
                .GroupBy(w => w.TournamentId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Name>)[.. g.Select(w => w.Name)]);

        var now = timeProvider.GetUtcNow();

        return [.. rows.Select(row =>
        {
            var sponsors = row.Sponsors
                .Select(s => new SeasonTournamentSponsorDto
                {
                    Name = s.Name,
                    Slug = s.Slug,
                    LogoUrl = s.LogoContainer is not null && s.LogoPath is not null
                        ? _fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath)
                        : null
                })
                .ToArray();

            var revealed = OilPatternRevealPolicy.IsRevealed(row.OilPatternRevealDateTime, query.CallerHasTournamentManagementPermission, now);

            return new SeasonTournamentDto
            {
                Id = row.Id,
                Name = row.Name,
                Season = row.Season,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                StatsEligible = row.StatsEligible,
                TournamentType = row.TournamentType,
                EntryFee = row.EntryFee,
                RegistrationUrl = row.RegistrationUrl,
                BowlingCenter = row.BowlingCenter,
                Sponsors = sponsors,
                AddedMoney = row.AddedMoney,
                Reservations = row.Reservations,
                PatternLengthCategory = row.PatternLengthCategory,
                PatternRatioCategory = row.PatternRatioCategory,
                OilPatternRevealDateTime = query.CallerIsAuthenticated ? row.OilPatternRevealDateTime : null,
                OilPatterns = revealed
                    ? row.OilPatterns.ConvertAll(pattern => new SeasonTournamentOilPatternDto
                    {
                        Name = pattern.Name,
                        Length = pattern.Length,
                        Volume = pattern.Volume,
                        LeftRatio = pattern.LeftRatio,
                        RightRatio = pattern.RightRatio,
                        KegelId = pattern.KegelId,
                        TournamentRounds = [.. pattern.TournamentRounds.Select(r => r.Name)]
                    })
                    : [],
                LogoUrl = row.TournamentLogoContainer is not null && row.TournamentLogoPath is not null
                    ? _fileStorageService.GetBlobUri(row.TournamentLogoContainer, row.TournamentLogoPath)
                    : null,
                Winners = historicalWinnersByTournamentDbId.GetValueOrDefault(row.DbId, []),
            };
        })];
    }
}
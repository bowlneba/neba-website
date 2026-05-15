using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Seasons.ListSeasons;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed partial class GetTournamentQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>
{
    private readonly IQueryable<Tournament> _tournaments
        = appDbContext.Tournaments.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampion
        = appDbContext.HistoricalTournamentChampions.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentEntry> _historicalTournamentEntries
        = appDbContext.HistoricalTournamentEntries.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentResult> _historicalTournamentResults
        = appDbContext.HistoricalTournamentResults.AsNoTracking();

    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<TournamentDetailDto>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        var row = await _tournaments
            .Where(tournament => tournament.Id == query.Id)
            .Select(tournament => new
            {
                DbId = EF.Property<int>(tournament, ShadowIdConfiguration.DefaultPropertyName),
                tournament.Id,
                tournament.Name,
                SeasonDescription = tournament.Season.Description,
                tournament.StartDate,
                tournament.EndDate,
                tournament.StatsEligible,
                TournamentType = tournament.TournamentType.Name,
                BowlingCenter = tournament.BowlingCenter == null
                    ? null
                    : new TournamentBowlingCenterDto
                    {
                        Name = tournament.BowlingCenter.Name,
                        City = tournament.BowlingCenter.Address.City,
                        State = tournament.BowlingCenter.Address.Region
                    },
                Sponsors = tournament.Sponsors
                    .Select(tournamentSponsor => tournamentSponsor.Sponsor)
                    .Select(s => new TournamentSponsorDto
                    {
                        Name = s.Name,
                        Slug = s.Slug,
                        LogoContainer = s.Logo!.Container,
                        LogoPath = s.Logo!.Path,
                        WebsiteUrl = s.WebsiteUrl,
                        TagPhrase = s.TagPhrase,
                    }).ToList(),
                AddedMoney = tournament.Sponsors.Sum(ts => ts.SponsorshipAmount),
                PatternLengthCategory = tournament.PatternLengthCategory == null
                    ? null
                    : tournament.PatternLengthCategory.Name,
                PatternRatioCategory = tournament.PatternRatioCategory == null
                    ? null
                    : tournament.PatternRatioCategory.Name,
                tournament.EntryFee,
                RegistrationUrl = tournament.ExternalRegistrationUrl,
                LogoContainer = tournament.Logo != null
                    ? tournament.Logo.Container
                    : null,
                LogoPath = tournament.Logo != null
                    ? tournament.Logo.Path
                    : null,
                Reservations = 999, // need to replace once actual column exists
                OilPatterns = tournament.OilPatterns.Select(top => new
                {
                    top.OilPattern.Name,
                    top.OilPattern.Length,
                    Rounds = top.TournamentRounds.Select(round => round.Name)
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);


        if (row is null)
        {
            return TournamentErrors.TournamentNotFound(query.Id);
        }

        var historicalWinners = await _historicalTournamentChampion
            .Where(tournamentChampion => tournamentChampion.TournamentId == row.DbId)
            .Select(tournamentChampion => tournamentChampion.Bowler.Name)
            .ToListAsync(cancellationToken);

        var historicalResults = await _historicalTournamentResults
            .Where(tournamentResult => tournamentResult.TournamentId == row.DbId)
            .Select(tournamentResult => new TournamentResultDto
            {
                BowlerName = tournamentResult.Bowler.Name,
                Place = tournamentResult.Place,
                PrizeMoney = tournamentResult.PrizeMoney,
                Points = tournamentResult.Points,
                SideCutName = tournamentResult.SideCut != null
                    ? tournamentResult.SideCut.Name
                    : null,
                SideCutIndicator = tournamentResult.SideCut != null
                    ? tournamentResult.SideCut.Indicator
                    : null,
            })
            .OrderBy(tournamentResult => tournamentResult.Place == null)
                .ThenBy(tournamentResult => tournamentResult.Place)
                .ThenBy(tournamentResult => tournamentResult.BowlerName.LastName)
                .ThenBy(tournamentResult => tournamentResult.BowlerName.FirstName)
            .ToListAsync(cancellationToken);

        var historicalEntryCount = await _historicalTournamentEntries
            .Where(tournamentEntry => tournamentEntry.TournamentId == row.DbId)
            .Select(tournamentEntry => (int?)tournamentEntry.Entries)
            .SingleOrDefaultAsync(cancellationToken);

        var tournament = new TournamentDetailDto
        {
            Id = row.Id,
            Name = row.Name,
            Season = row.SeasonDescription,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            StatsEligible = row.StatsEligible,
            TournamentType = row.TournamentType,
            EntryFee = row.EntryFee,
            RegistrationUrl = row.RegistrationUrl,
            BowlingCenter = row.BowlingCenter,
            Sponsors = row.Sponsors,
            AddedMoney = row.AddedMoney,
            Reservations = row.Reservations,
            PatternLengthCategory = row.PatternLengthCategory,
            PatternRatioCategory = row.PatternRatioCategory,
            OilPatterns = row.OilPatterns.ConvertAll(pattern => new TournamentOilPatternDto
            {
                Name = pattern.Name,
                Length = pattern.Length,
                TournamentRounds = [.. pattern.Rounds]
            }),
            LogoContainer = row.LogoContainer,
            LogoPath = row.LogoPath,
            Winners = historicalWinners,
            // If Results or EntryCount are empty/null, check future stats tables for 2026+ tournament data
            Results = historicalResults,
            EntryCount = historicalEntryCount,
        };

        if (tournament.LogoContainer is not null && tournament.LogoPath is not null)
        {
            tournament = tournament with { LogoUrl = _fileStorageService.GetBlobUri(tournament.LogoContainer, tournament.LogoPath) };
        }

        var sponsors = tournament.Sponsors
            .Select(s => s.LogoContainer is not null && s.LogoPath is not null
                ? s with { LogoUrl = _fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath) }
                : s)
            .ToArray();

        return tournament with { Sponsors = sponsors };
    }
}
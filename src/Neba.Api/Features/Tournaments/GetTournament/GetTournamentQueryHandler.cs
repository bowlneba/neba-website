using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Entities;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed class GetTournamentQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService,
    TimeProvider timeProvider)
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
    private readonly IQueryable<SquadScore> _squadScores
        = appDbContext.SquadScores.AsNoTracking();

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
                TournamentTypeValue = tournament.TournamentType.Value,
                SquadIds = tournament.Squads.Select(squad => squad.Id).ToList(),
                BowlingCenter = tournament.BowlingCenter == null
                    ? null
                    : new TournamentDetailBowlingCenterDto
                    {
                        Name = tournament.BowlingCenter.Name,
                        City = tournament.BowlingCenter.Address.City,
                        State = tournament.BowlingCenter.Address.Region,
                        CertificationNumber = tournament.BowlingCenterId != null ? tournament.BowlingCenterId.Value : null
                    },
                Sponsors = tournament.Sponsors
                    .Select(tournamentSponsor => new
                    {
                        tournamentSponsor.Sponsor.Name,
                        tournamentSponsor.Sponsor.Slug,
                        LogoContainer = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Container : null,
                        LogoPath = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Path : null,
                        tournamentSponsor.Sponsor.WebsiteUrl,
                        tournamentSponsor.Sponsor.TagPhrase,
                        tournamentSponsor.SponsorId,
                        tournamentSponsor.TitleSponsor,
                        tournamentSponsor.SponsorshipAmount
                    }).ToList(),
                SponsorMoney = tournament.Sponsors.Sum(ts => ts.SponsorshipAmount),
                tournament.NebaAddedMoney,
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
                TournamentLogoContentType = tournament.Logo != null
                    ? tournament.Logo.ContentType
                    : null,
                TournamentLogoSizeInBytes = tournament.Logo != null
                    ? (long?)tournament.Logo.SizeInBytes
                    : null,
                Reservations = 999, // need to replace once actual column exists
                OilPatterns = tournament.OilPatterns.Select(top => new
                {
                    top.OilPatternId,
                    top.OilPattern.Name,
                    top.OilPattern.Length,
                    top.OilPattern.Volume,
                    top.OilPattern.LeftRatio,
                    top.OilPattern.RightRatio,
                    top.TournamentRounds,
                    top.OilPattern.KegelId
                }).ToList(),
                Articles = tournament.Articles
                    .Where(a => a.PublicationStatus == PublicationStatus.Published)
                    .Select(a => new TournamentDetailArticleDto
                    {
                        Title = a.Title,
                        Slug = a.Slug,
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

        var recordedResults = await _tournaments
            .Where(tournament => tournament.Id == query.Id)
            .SelectMany(tournament => tournament.Results)
            .Select(result => new TournamentResultDto
            {
                BowlerName = result.Bowler.Name,
                Place = result.Place,
                PrizeMoney = result.PrizeMoney,
                Points = result.Points,
                SideCutName = null,
                SideCutIndicator = null,
            })
            .ToListAsync(cancellationToken);

        var recordedWinners = recordedResults
            .Where(result => result.Place == 1)
            .Select(result => result.BowlerName)
            .ToList();

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
            .ToListAsync(cancellationToken);

        var results = historicalResults.Concat(recordedResults)
            .OrderBy(tournamentResult => tournamentResult.Place == null)
                .ThenBy(tournamentResult => tournamentResult.Place)
                .ThenBy(tournamentResult => tournamentResult.BowlerName.LastName)
                .ThenBy(tournamentResult => tournamentResult.BowlerName.FirstName)
            .ToList();

        var winners = historicalWinners.Concat(recordedWinners).ToList();

        var historicalEntryCount = await _historicalTournamentEntries
            .Where(tournamentEntry => tournamentEntry.TournamentId == row.DbId)
            .Select(tournamentEntry => (int?)tournamentEntry.Entries)
            .SingleOrDefaultAsync(cancellationToken);

        var recordedEntryPairCount = await _squadScores
            .Where(squadScore => row.SquadIds.Contains(squadScore.SquadId))
            .Select(squadScore => new { squadScore.SquadId, squadScore.BowlerId })
            .Distinct()
            .CountAsync(cancellationToken);

        var teamSize = TournamentType.FromValue(row.TournamentTypeValue).TeamSize;
        var recordedEntryCount = recordedEntryPairCount > 0
            ? recordedEntryPairCount / teamSize
            : (int?)null;

        var entryCount = historicalEntryCount ?? recordedEntryCount;

        var sponsors = row.Sponsors
            .Select(s => new TournamentDetailSponsorDto
            {
                Name = s.Name,
                Slug = s.Slug,
                LogoUrl = s.LogoContainer is not null && s.LogoPath is not null
                    ? _fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath)
                    : null,
                WebsiteUrl = s.WebsiteUrl,
                TagPhrase = s.TagPhrase,
                SponsorId = s.SponsorId,
                TitleSponsor = s.TitleSponsor,
                SponsorshipAmount = s.SponsorshipAmount,
            })
            .OrderByDescending(s => s.TitleSponsor)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var revealed = OilPatternRevealPolicy.IsRevealed(
            row.OilPatternRevealDateTime, query.CallerHasTournamentManagementPermission, timeProvider.GetUtcNow());

        var bowlingCenter = row.BowlingCenter;
        if (bowlingCenter is not null && !query.CallerHasTournamentManagementPermission)
        {
            bowlingCenter = bowlingCenter with { CertificationNumber = null };
        }

        return new TournamentDetailDto
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
            BowlingCenter = bowlingCenter,
            Sponsors = sponsors,
            AddedMoney = row.SponsorMoney + row.NebaAddedMoney,
            SponsorMoney = row.SponsorMoney,
            NebaAddedMoney = row.NebaAddedMoney,
            Reservations = row.Reservations,
            PatternLengthCategory = row.PatternLengthCategory,
            PatternRatioCategory = row.PatternRatioCategory,
            OilPatternRevealDateTime = query.CallerIsAuthenticated ? row.OilPatternRevealDateTime : null,
            OilPatterns = revealed
                ? row.OilPatterns.ConvertAll(pattern => new TournamentDetailOilPatternDto
                {
                    OilPatternId = pattern.OilPatternId,
                    Name = pattern.Name,
                    Length = pattern.Length,
                    Volume = pattern.Volume,
                    LeftRatio = pattern.LeftRatio,
                    RightRatio = pattern.RightRatio,
                    TournamentRounds = [.. pattern.TournamentRounds.Select(r => r.Name)],
                    KegelId = pattern.KegelId,
                })
                : [],
            LogoUrl = row.TournamentLogoContainer is not null && row.TournamentLogoPath is not null
                ? _fileStorageService.GetBlobUri(row.TournamentLogoContainer, row.TournamentLogoPath)
                : null,
            LogoContainer = query.CallerHasTournamentManagementPermission ? row.TournamentLogoContainer : null,
            LogoPath = query.CallerHasTournamentManagementPermission ? row.TournamentLogoPath : null,
            LogoContentType = query.CallerHasTournamentManagementPermission ? row.TournamentLogoContentType : null,
            LogoSizeInBytes = query.CallerHasTournamentManagementPermission ? row.TournamentLogoSizeInBytes : null,
            Winners = winners,
            Results = results,
            EntryCount = entryCount,
            Articles = row.Articles,
        };
    }
}
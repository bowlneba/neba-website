using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed class GetSeriesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<GetSeriesQuery, ErrorOr<SeriesDetailDto>>
{
    public async Task<ErrorOr<SeriesDetailDto>> HandleAsync(GetSeriesQuery query, CancellationToken cancellationToken)
    {
        var header = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == query.Id)
            .Join(appDbContext.Seasons.AsNoTracking(), series => series.SeasonId, season => season.Id,
                (series, season) => new
                {
                    series.Id,
                    series.Name,
                    SeasonId = season.Id,
                    season.Description,
                    season.StartDate,
                    season.EndDate,
                    IsLocked = series.Stages.Any(s => s.Status != StageStatus.Pending),
                    series.Logo,
                    series.Finale,
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return SeriesErrors.NotFound(query.Id);
        }

        var stages = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == query.Id)
            .SelectMany(series => series.Stages)
            .Join(appDbContext.Tournaments.AsNoTracking(), stage => stage.TournamentId, tournament => tournament.Id,
                (stage, tournament) => new { stage, tournament })
            .OrderBy(x => x.stage.Order)
            .Select(x => new
            {
                x.stage.Id,
                x.stage.Order,
                x.stage.Kind,
                x.stage.Status,
                x.stage.AdvancingCount,
                SquadDate = x.stage.SquadDesignation == null ? (DateOnly?)null : x.stage.SquadDesignation.Date,
                SquadStartTime = x.stage.SquadDesignation == null ? (TimeOnly?)null : x.stage.SquadDesignation.StartTime,
                TournamentId = x.tournament.Id,
                TournamentName = x.tournament.Name,
                x.tournament.StartDate,
                x.tournament.EndDate,
                x.tournament.TournamentType,
            })
            .ToListAsync(cancellationToken);

        // The Finale's host center, when one is set (no navigation from the owned Finale, so a second small query).
        SeriesFinaleDto? finale = null;
        if (header.Finale is { } storedFinale)
        {
            var center = storedFinale.BowlingCenterId is { } centerId
                ? await appDbContext.BowlingCenters.AsNoTracking()
                    .Where(bowlingCenter => bowlingCenter.CertificationNumber == centerId)
                    .Select(bowlingCenter => new { bowlingCenter.Name, bowlingCenter.Address.City, State = bowlingCenter.Address.Region })
                    .SingleOrDefaultAsync(cancellationToken)
                : null;

            finale = new SeriesFinaleDto
            {
                FinalistCount = storedFinale.FinalistCount,
                Date = storedFinale.Date,
                StartTime = storedFinale.StartTime,
                BowlingCenterId = storedFinale.BowlingCenterId?.Value,
                BowlingCenterName = center?.Name,
                BowlingCenterCity = center?.City,
                BowlingCenterState = center?.State,
            };
        }

        return new SeriesDetailDto
        {
            Id = header.Id,
            Name = header.Name,
            SeasonId = header.SeasonId,
            SeasonDescription = header.Description,
            SeasonStartDate = header.StartDate,
            SeasonEndDate = header.EndDate,
            IsLocked = header.IsLocked,
            Finale = finale,
            LogoUrl = header.Logo is null ? null : fileStorageService.GetBlobUri(header.Logo.Container, header.Logo.Path),
            LogoContainer = header.Logo?.Container,
            LogoPath = header.Logo?.Path,
            LogoContentType = header.Logo?.ContentType,
            LogoSizeInBytes = header.Logo?.SizeInBytes,
            Stages = [.. stages.Select(s => new SeriesStageDto
            {
                Id = s.Id,
                Order = s.Order,
                Kind = s.Kind.Name,
                Status = s.Status.Name,
                AdvancingCount = s.AdvancingCount,
                SquadDate = s.SquadDate,
                SquadStartTime = s.SquadStartTime,
                TournamentId = s.TournamentId,
                TournamentName = s.TournamentName,
                TournamentStartDate = s.StartDate,
                TournamentEndDate = s.EndDate,
                TournamentType = s.TournamentType.Name,
                TeamSize = s.TournamentType.TeamSize,
            })],
        };
    }
}
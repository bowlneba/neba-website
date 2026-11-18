using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;
using Neba.Api.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Series.CreateSeries;

internal sealed class CreateSeriesCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateSeriesCommand, SeriesId>
{
    public async Task<ErrorOr<SeriesId>> HandleAsync(CreateSeriesCommand command, CancellationToken cancellationToken)
    {
        if (!await appDbContext.Seasons.AnyAsync(season => season.Id == command.SeasonId, cancellationToken))
        {
            return SeriesErrors.SeasonNotFound(command.SeasonId.ToString());
        }

        var finaleCenter = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(appDbContext, command.Finale, currentCenterId: null, cancellationToken);
        if (finaleCenter.IsError)
        {
            return finaleCenter.Errors;
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], excludingSeriesId: null, cancellationToken);

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, current: null, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        var series = Domain.Series.Create(command.Name, command.SeasonId, command.Logo, command.Stages, command.Finale, tournaments);
        if (series.IsError)
        {
            return series.Errors;
        }

        await appDbContext.Series.AddAsync(series.Value, cancellationToken);
        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, token: cancellationToken);

        return series.Value.Id;
    }
}
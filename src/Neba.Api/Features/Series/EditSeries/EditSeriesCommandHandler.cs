using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.Api.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Series.EditSeries;

internal sealed class EditSeriesCommandHandler(
        AppDbContext appDbContext,
        IPermissionDenialReader permissionDenialReader,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<EditSeriesCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSeriesCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series
            .Include(s => s.Stages)
            .SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);

        if (series is null)
        {
            return SeriesErrors.NotFound(command.SeriesId);
        }

        // Same rule as EditTournament: a stored denial of the logo permission blocks changing the logo here too.
        var logoChanged = !StoredFileClaim.IsSameFile(command.Logo, series.Logo);
        if (logoChanged && await permissionDenialReader.IsDeniedAsync(
                command.CallerUserId, Contracts.Security.Permissions.EditSeriesLogo, cancellationToken))
        {
            return SeriesErrors.LogoChangeDenied;
        }

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, series.Logo, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        var finaleCenter = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(
            appDbContext, command.Finale, series.Finale?.BowlingCenterId, cancellationToken);
        if (finaleCenter.IsError)
        {
            return finaleCenter.Errors;
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], command.SeriesId, cancellationToken);

        var renamed = series.Rename(command.Name);
        var replaced = series.ReplaceConfiguration(command.Stages, command.Finale, tournaments);

        List<Error> errors = [.. renamed.ErrorsOrEmptyList, .. replaced.ErrorsOrEmptyList];
        if (errors.Count > 0)
        {
            return errors;
        }

        // Snapshot before ChangeLogo: the property is replaced in place.
        var previousLogo = series.Logo;
        var logo = StoredFileClaim.Resolve(command.Logo, previousLogo);
        series.ChangeLogo(logo); // allowed while locked

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, logo, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, token: cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousLogo, logo);

        return Result.Updated;
    }
}

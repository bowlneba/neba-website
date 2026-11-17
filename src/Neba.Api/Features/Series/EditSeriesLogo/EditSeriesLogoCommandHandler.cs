using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;
using Neba.Api.Uploads;

namespace Neba.Api.Features.Series.EditSeriesLogo;

internal sealed class EditSeriesLogoCommandHandler(AppDbContext appDbContext, IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<EditSeriesLogoCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSeriesLogoCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series.SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);
        if (series is null)
        {
            return SeriesErrors.NotFound(command.SeriesId);
        }

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, series.Logo, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        // Snapshot before ChangeLogo: the property is replaced in place.
        var previousLogo = series.Logo;
        var logo = StoredFileClaim.Resolve(command.Logo, previousLogo);
        series.ChangeLogo(logo); // allowed while locked

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, logo, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousLogo, logo);

        return Result.Updated;
    }
}

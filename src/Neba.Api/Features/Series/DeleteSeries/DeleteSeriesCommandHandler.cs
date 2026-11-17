using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Uploads;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed class DeleteSeriesCommandHandler(AppDbContext appDbContext, IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<DeleteSeriesCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteSeriesCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series
            .Include(s => s.Stages)
            .SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Deleted;
        }

        var canDelete = series.EnsureCanDelete();
        if (canDelete.IsError)
        {
            return canDelete.Errors;
        }

        var logo = series.Logo;

        appDbContext.Series.Remove(series);
        await appDbContext.SaveChangesAsync(cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, logo, current: null);

        return Result.Deleted;
    }
}

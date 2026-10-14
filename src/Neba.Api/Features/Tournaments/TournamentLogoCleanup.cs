using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.EditTournament;

namespace Neba.Api.Features.Tournaments;

internal static class TournamentLogoCleanup
{
    /// <summary>
    /// Queues deletion of the previous logo file when the tournament no longer points at it.
    /// Files are the same when container and path match; content type and size do not count.
    /// </summary>
    public static void ScheduleDeleteOfReplacedLogo(
        IBackgroundJobScheduler backgroundJobScheduler,
        StoredFile? previousLogo,
        StoredFile? currentLogo)
    {
        if (previousLogo is null
            || (currentLogo is not null
                && currentLogo.Container == previousLogo.Container
                && currentLogo.Path == previousLogo.Path))
        {
            return;
        }

        backgroundJobScheduler.Enqueue(new DeleteTournamentFilesJob
        {
            Files =
            [
                new TournamentFileReference { Container = previousLogo.Container, Path = previousLogo.Path }
            ]
        });
    }
}

using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Seasons.CreateNextSeason;

internal sealed record CreateNextSeasonJob
    : IBackgroundJob
{
    public string JobName
        => "Create Next Season Job";
}

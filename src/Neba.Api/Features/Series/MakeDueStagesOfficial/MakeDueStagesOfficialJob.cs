using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Series.MakeDueStagesOfficial;

internal sealed record MakeDueStagesOfficialJob
    : IBackgroundJob
{
    public string JobName
        => "Make Due Series Stages Official";
}
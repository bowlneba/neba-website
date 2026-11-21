using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed class RecomputeSeriesAdvancementCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<RecomputeSeriesAdvancementCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(RecomputeSeriesAdvancementCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.Recompute(command.StageId, command.ConfirmResetOfficial, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait,
            needsFacts: true,
            cancellationToken);

        return result.IsError ? result.Errors : Result.Updated;
    }
}
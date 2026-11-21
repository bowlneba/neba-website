using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<RemoveStageOverrideCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RemoveStageOverrideCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.RemoveOverride(command.StageId, command.OverrideId, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait,
            cancellationToken);

        return result.IsError ? result.Errors : Result.Deleted;
    }
}
using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.PickStageSquad;

internal sealed class PickStageSquadCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<PickStageSquadCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(PickStageSquadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.PickSquad(command.StageId, command.SquadId, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait,
            cancellationToken);

        return result.IsError ? result.Errors : Result.Updated;
    }
}
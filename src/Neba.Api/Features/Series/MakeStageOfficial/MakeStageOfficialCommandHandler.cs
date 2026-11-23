using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed class MakeStageOfficialCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<MakeStageOfficialCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(MakeStageOfficialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await advancement.RunWithoutFactsAsync(
            command.SeriesId,
            (series, _, now) => series.MakeOfficial(command.StageId, command.Version, now),
            SeriesAdvancement.AdminLockWait,
            cancellationToken);

        return result.IsError ? result.Errors : Result.Updated;
    }
}

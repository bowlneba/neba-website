using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.CreateStageOverride;

internal sealed class CreateStageOverrideCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride>
{
    public async Task<ErrorOr<CreatedStageOverride>> HandleAsync(CreateStageOverrideCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var createdBy = command.CallerUserId.ToString();

        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => command.Kind == OverrideKind.Add
                ? series.AddOverride(command.StageId, command.BowlerId, command.Reason, createdBy, command.Version, facts, now)
                : series.ClearSlot(command.StageId, command.BowlerId, command.Reason, createdBy, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait,
            needsFacts: true,
            cancellationToken);

        return result.IsError
            ? result.Errors
            : new CreatedStageOverride(result.Value.OverrideId!.Value, result.Value.Version);
    }
}
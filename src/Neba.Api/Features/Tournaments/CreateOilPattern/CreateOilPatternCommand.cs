using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed record CreateOilPatternCommand
    : ICommand<CreatedOilPattern>
{
    public required string Name { get; init; }

    public required int Length { get; init; }

    public required decimal Volume { get; init; }

    public required decimal LeftRatio { get; init; }

    public required decimal RightRatio { get; init; }

    public Guid? KegelId { get; init; }
}
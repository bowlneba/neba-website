using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed record BowlerSearchSummary
{
    public required BowlerId Id { get; init; }

    public required Name Name { get; init; }

    public string? UsbcId { get; init; }
}

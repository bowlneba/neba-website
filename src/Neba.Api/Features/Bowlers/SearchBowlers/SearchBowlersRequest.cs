namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed record SearchBowlersRequest
{
    public string Name { get; init; } = string.Empty;
}

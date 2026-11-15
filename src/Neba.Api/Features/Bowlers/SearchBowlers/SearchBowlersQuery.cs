using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

/// <summary>
/// Asks for the bowlers whose name contains the given text. Not cached: the text varies with every keystroke.
/// </summary>
internal sealed record SearchBowlersQuery
    : IQuery<IReadOnlyCollection<BowlerSearchSummary>>
{
    public required string Name { get; init; }
}
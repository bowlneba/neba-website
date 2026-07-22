using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed class ListTournamentTypesQueryHandler
    : IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>>
{
    public Task<IReadOnlyCollection<TournamentTypeSummaryDto>> HandleAsync(ListTournamentTypesQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<TournamentTypeSummaryDto>>(
        [.. TournamentType.List
            .Where(t => t.ActiveFormat)
            .Select(t => new TournamentTypeSummaryDto { Name = t.Name })]);
}
using Neba.Api.Contacts.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.ReferenceData.ListUsStates;

internal sealed class ListUsStatesQueryHandler
    : IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>>
{
    public Task<IReadOnlyCollection<UsStateDto>> HandleAsync(ListUsStatesQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<UsStateDto>>(
            [.. UsState.List.Select(state => new UsStateDto { Name = state.Name, Code = state.Value })]);
}
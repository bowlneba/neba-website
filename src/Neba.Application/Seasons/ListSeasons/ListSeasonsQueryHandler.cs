using Neba.Application.Messaging;

namespace Neba.Application.Seasons.ListSeasons;

internal sealed class ListSeasonsQueryHandler(ISeasonQueries seasonQuery)
        : IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>
{
    private readonly ISeasonQueries _seasonQuery = seasonQuery;

    public Task<IReadOnlyCollection<SeasonDto>> HandleAsync(ListSeasonsQuery query, CancellationToken cancellationToken)
        => _seasonQuery.GetAllAsync(cancellationToken);
}
using Neba.Application.Messaging;

namespace Neba.Application.Seasons.ListSeasons;

internal sealed class ListSeasonsQueryHandler(ISeasonQueries seasonQuery)
        : IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>
{
    private readonly ISeasonQueries _seasonQuery = seasonQuery;

    public async Task<IReadOnlyCollection<SeasonDto>> HandleAsync(ListSeasonsQuery query, CancellationToken cancellationToken)
    {
        var seasons = await _seasonQuery.GetAllAsync(cancellationToken);

        return seasons;
    }
}
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListChampions;

internal sealed class ListChampionsEndpoint(
    IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<TournamentChampionResponse>>
{
    private readonly IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("champions");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListChampions")
            .WithTags("Public")
            .Produces<CollectionResponse<TournamentChampionResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListChampionsQuery(), ct);

        var response = new CollectionResponse<TournamentChampionResponse>
        {
            Items = [.. result.Select(t => new TournamentChampionResponse
            {
                TournamentId = t.TournamentId.Value.ToString(),
                TournamentName = t.TournamentName,
                TournamentDate = t.TournamentDate,
                TournamentType = t.TournamentType.Name,
                Champions = [.. t.Champions.Select(c => new ChampionResponse
                {
                    BowlerId = c.BowlerId.Value.ToString(),
                    BowlerName = c.BowlerName.ToDisplayName(),
                    HallOfFame = c.HallOfFame,
                })],
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
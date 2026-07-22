using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Tournaments.ListTournamentTypes;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed class ListTournamentTypesEndpoint(IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<TournamentTypeSummaryResponse>>
{
    private readonly IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("types");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListTournamentTypes")
            .WithTags("Public")
            .Produces<CollectionResponse<TournamentTypeSummaryResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListTournamentTypesQuery(), ct);

        var response = new CollectionResponse<TournamentTypeSummaryResponse>
        {
            Items = [.. result.Select(t => new TournamentTypeSummaryResponse { Name = t.Name })]
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
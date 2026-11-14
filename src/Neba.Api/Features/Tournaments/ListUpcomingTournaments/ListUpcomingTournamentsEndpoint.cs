using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Security;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.ListUpcomingTournaments;

internal sealed class ListUpcomingTournamentsEndpoint(
    IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<SeasonTournamentResponse>>
{
    private readonly IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("upcoming");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListUpcomingTournaments")
            .WithTags("Public")
            .Produces<CollectionResponse<SeasonTournamentResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new ListUpcomingTournamentsQuery
        {
            CallerIsAuthenticated = User.Identity?.IsAuthenticated == true,
            CallerHasTournamentManagementPermission = User.HasAnyPermission(PermissionCatalog.TournamentManagementPermissions)
        };
        var result = await _queryHandler.HandleAsync(query, ct);

        var response = new CollectionResponse<SeasonTournamentResponse>
        {
            Items = [.. result.Select(t => t.ToResponse())],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Features.Seasons;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonEndpoint(
    IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>> queryHandler)
    : Endpoint<ListTournamentsInSeasonRequest, CollectionResponse<SeasonTournamentResponse>>
{
    private readonly IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{seasonId}/tournaments");
        Group<SeasonsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Seasons")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListTournamentsInSeason")
            .WithTags("Public")
            .Produces<CollectionResponse<SeasonTournamentResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(ListTournamentsInSeasonRequest req, CancellationToken ct)
    {
        var query = new ListTournamentsInSeasonQuery
        {
            SeasonId = new SeasonId(req.SeasonId),
            CallerIsAuthenticated = User.Identity?.IsAuthenticated == true,
            CallerHasTournamentManagementPermission = User.HasAnyPermission(PermissionCatalog.TournamentManagementPermissions)
        };
        var result = await _queryHandler.HandleAsync(query, ct);

        var response = new CollectionResponse<SeasonTournamentResponse>
        {
            Items = [.. result.Select(t => new SeasonTournamentResponse
            {
                Id = t.Id.Value.ToString(),
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                TournamentType = t.TournamentType,
                EntryFee = t.EntryFee,
                RegistrationUrl = t.RegistrationUrl,
                AddedMoney = t.AddedMoney,
                SponsorMoney = t.SponsorMoney,
                NebaAddedMoney = t.NebaAddedMoney,
                Reservations = t.Reservations,
                PatternLengthCategory = t.PatternLengthCategory,
                PatternRatioCategory = t.PatternRatioCategory,
                OilPatternRevealDateTime = t.OilPatternRevealDateTime,
                LogoUrl = t.LogoUrl,
                Winners = [.. t.Winners.Select(w => w.ToDisplayName())],
                BowlingCenter = t.BowlingCenter is null ? null : new TournamentBowlingCenterResponse
                {
                    Name = t.BowlingCenter.Name,
                    City = t.BowlingCenter.City,
                    State = t.BowlingCenter.State,
                },
                Sponsors = [.. t.Sponsors.Select(s => new TournamentSponsorResponse
                {
                    Name = s.Name,
                    Slug = s.Slug,
                    LogoUrl = s.LogoUrl,
                })],
                OilPatterns = [.. t.OilPatterns.Select(op => new TournamentOilPatternResponse
                {
                    Name = op.Name,
                    Length = op.Length,
                    Volume = op.Volume,
                    LeftRatio = op.LeftRatio,
                    RightRatio = op.RightRatio,
                    KegelId = op.KegelId,
                    Rounds = op.TournamentRounds,
                })],
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
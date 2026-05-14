using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Application.Messaging;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Seasons.ListTournamentsInSeason;

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
        var query = new ListTournamentsInSeasonQuery { SeasonId = new SeasonId(req.SeasonId) };
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
                Reservations = t.Reservations,
                PatternLengthCategory = t.PatternLengthCategory,
                PatternRatioCategory = t.PatternRatioCategory,
                LogoUrl = t.LogoUrl,
                Winners = [.. t.Winners.Select(w => w.ToDisplayName())],
                BowlingCenter = t.BowlingCenter is null ? null : new TournamentBowlingCenterResponse
                {
                    Name = t.BowlingCenter.Name,
                    City = t.BowlingCenter.Address.City,
                    State = t.BowlingCenter.Address.Region,
                },
                Sponsors = [.. t.Sponsors.Select(s => new TournamentSponsorResponse
                {
                    Name = s.Name,
                    Slug = s.Slug,
                    LogoUrl = s.LogoUrl,
                })],
                OilPatterns = [.. t.OilPatterns.Select(op => new TournamentOilPatternResponse
                {
                    Name = op.OilPattern.Name,
                    Length = op.OilPattern.Length,
                    Volume = op.OilPattern.Volume,
                    LeftRatio = op.OilPattern.LeftRatio,
                    RightRatio = op.OilPattern.RightRatio,
                    KegelId = op.OilPattern.KegelId,
                    Rounds = op.TournamentRounds,
                })],
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
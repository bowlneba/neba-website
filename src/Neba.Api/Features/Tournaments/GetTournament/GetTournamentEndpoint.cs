using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed class GetTournamentEndpoint(
    IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>> queryHandler)
    : Endpoint<GetTournamentRequest, TournamentDetailResponse>
{
    private readonly IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetTournament")
            .WithTags("Public")
            .Produces<TournamentDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(GetTournamentRequest req, CancellationToken ct)
    {
        var query = new GetTournamentQuery { Id = new TournamentId(req.TournamentId) };
        var result = await _queryHandler.HandleAsync(query, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        var response = new TournamentDetailResponse
        {
            Id = dto.Id.Value.ToString(),
            Name = dto.Name,
            Season = dto.Season,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StatsEligible = dto.StatsEligible,
            TournamentType = dto.TournamentType,
            EntryFee = dto.EntryFee,
            RegistrationUrl = dto.RegistrationUrl,
            AddedMoney = dto.AddedMoney,
            Reservations = dto.Reservations,
            EntryCount = dto.EntryCount,
            PatternLengthCategory = dto.PatternLengthCategory,
            PatternRatioCategory = dto.PatternRatioCategory,
            LogoUrl = dto.LogoUrl,
            BowlingCenter = dto.BowlingCenter is null ? null : new TournamentDetailBowlingCenterResponse
            {
                Name = dto.BowlingCenter.Name,
                City = dto.BowlingCenter.City,
                State = dto.BowlingCenter.State,
            },
            Sponsors = [.. dto.Sponsors.Select(s => new TournamentDetailSponsorResponse
            {
                Name = s.Name,
                Slug = s.Slug,
                LogoUrl = s.LogoUrl,
                WebsiteUrl = s.WebsiteUrl,
                TagPhrase = s.TagPhrase,
            })],
            OilPatterns = [.. dto.OilPatterns.Select(op => new TournamentDetailOilPatternResponse
            {
                Name = op.Name,
                Length = op.Length,
                Rounds = op.TournamentRounds,
            })],
            Winners = [.. dto.Winners.Select(w => w.ToDisplayName())],
            Results = [.. dto.Results.Select(r => new TournamentResultResponse
            {
                BowlerName = r.BowlerName.ToDisplayName(),
                Place = r.Place,
                PrizeMoney = r.PrizeMoney,
                Points = r.Points,
                SideCutName = r.SideCutName,
                SideCutIndicator = r.SideCutIndicator is { } c
                    ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                    : null,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Bowlers.GetBowlerTitles;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesEndpoint(
    IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>> queryHandler)
    : Endpoint<GetBowlerTitlesRequest, BowlerTitlesResponse>
{
    private readonly IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/titles");
        Group<BowlersEndpointGroup>();

        Options(options => options
            .WithVersionSet("Bowlers")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetBowlerTitles")
            .WithTags("Public")
            .Produces<BowlerTitlesResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(GetBowlerTitlesRequest req, CancellationToken ct)
    {
        var query = new GetBowlerTitlesQuery { BowlerId = new Domain.BowlerId(req.BowlerId) };
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

        var response = new BowlerTitlesResponse
        {
            BowlerName = dto.BowlerName.ToDisplayName(),
            HallOfFame = dto.HallOfFame,
            Titles = [.. dto.Titles.Select(title => new BowlerTitleResponse
            {
                TournamentId = title.TournamentId.Value.ToString(),
                TournamentName = title.TournamentName,
                TournamentDate = title.TournamentDate,
                TournamentType = title.TournamentType,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}

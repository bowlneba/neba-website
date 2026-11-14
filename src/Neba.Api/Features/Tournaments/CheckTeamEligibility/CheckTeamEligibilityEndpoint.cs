using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed class CheckTeamEligibilityEndpoint(
    IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>> queryHandler)
    : Endpoint<CheckTeamEligibilityRequest, TeamEligibilityResponse>
{
    public override void Configure()
    {
        Get("{id}/team-eligibility");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("CheckTeamEligibility")
            .WithTags("Public")
            .Produces<TeamEligibilityResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CheckTeamEligibilityRequest req, CancellationToken ct)
    {
        var result = await queryHandler.HandleAsync(
            new CheckTeamEligibilityQuery
            {
                TournamentId = new TournamentId(req.Id),
                BowlerIds = [.. req.BowlerIds.Select(id => new BowlerId(id))],
                UnknownBowlerCount = req.UnknownBowlerCount
            },
            ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description, error.Code);
            }

            await Send.ErrorsAsync(
                result.FirstError.Type switch
                {
                    ErrorType.NotFound => StatusCodes.Status404NotFound,
                    ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
                    _ => StatusCodes.Status500InternalServerError
                },
                ct);

            return;
        }

        var dto = result.Value;

        await Send.OkAsync(
            new TeamEligibilityResponse
            {
                Outcome = dto.Outcome.Name,
                Reasons = [.. dto.Reasons.Select(ToResponse)],
                DateChecks = [.. dto.DateChecks.Select(check => new TeamEligibilityDateCheckResponse
                {
                    AsOf = check.AsOf,
                    Eligible = check.Eligible,
                    Reasons = [.. check.Reasons.Select(ToResponse)]
                })],
                DefendingSource = dto.DefendingTeam is { } team
                    ? new DefendingSourceResponse
                    {
                        TournamentName = team.SourceTournamentName,
                        SeasonDescription = team.SeasonDescription,
                        SourceTruncated = team.SourceTruncated,
                        Undefended = team.Undefended
                    }
                    : null,

                // Pro titles are never checked; the page shows a note when the team is otherwise eligible.
                ProfessionalTitlesVerified = false
            },
            ct);
    }

    private static TeamEligibilityReasonResponse ToResponse(TeamEligibilityReason reason)
        => new() { Code = reason.Code, Message = reason.Message };
}

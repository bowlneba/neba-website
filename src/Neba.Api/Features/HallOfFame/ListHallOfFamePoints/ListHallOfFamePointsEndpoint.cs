using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed class ListHallOfFamePointsEndpoint(IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto> queryHandler)
    : EndpointWithoutRequest<HallOfFamePointsResponse>
{
    private readonly IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("points");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListHallOfFamePoints")
            .WithTags("Public")
            .Produces<HallOfFamePointsResponse>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var dto = await _queryHandler.HandleAsync(new ListHallOfFamePointsQuery(), ct);

        var response = new HallOfFamePointsResponse
        {
            Rules = [.. dto.Rules.Select(rule => new HallOfFamePointRuleResponse
            {
                Group = rule.Group,
                Label = rule.Label,
                Points = rule.Points
            })],
            RequiredPoints = dto.RequiredPoints,
            RequiredSeasonsWithPoints = dto.RequiredSeasonsWithPoints,
            RequiredMembershipYears = dto.RequiredMembershipYears,
            MembershipDataStartYear = dto.MembershipDataStartYear,
            Standings = [.. dto.Standings.Select(standing => new HallOfFamePointsStandingResponse
            {
                BowlerId = standing.BowlerId,
                BowlerName = standing.BowlerName,
                TotalPoints = standing.TotalPoints,
                SeasonsWithPoints = standing.SeasonsWithPoints,
                MembershipYears = standing.MembershipYears,
                MembershipHistoryComplete = standing.MembershipHistoryComplete,
                Eligible = standing.Eligible,
                MeetsPoints = standing.MeetsPoints,
                MeetsSeasons = standing.MeetsSeasons,
                InductedCategories = standing.InductedCategories
            })]
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
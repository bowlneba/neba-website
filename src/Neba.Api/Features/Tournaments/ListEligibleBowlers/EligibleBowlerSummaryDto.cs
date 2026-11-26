namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>A bowler who qualifies for an Invitational: display name and USBC ID only.</summary>
internal sealed record EligibleBowlerSummaryDto
{
    public required string Name { get; init; }

    public string? UsbcId { get; init; }
}

using FastEndpoints;

namespace Neba.Api.Features.Bowlers.GetBowlerTitles;

internal sealed class GetBowlerTitlesRequest
{
    [BindFrom("id")]
    public required string BowlerId { get; set; }
}
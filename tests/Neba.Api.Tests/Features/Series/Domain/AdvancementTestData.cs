using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Tests.Features.Series.Domain;

internal static class AdvancementTestData
{
    /// <summary>New bowler IDs in ascending order, so a test can predict the BowlerId tie-break.</summary>
    public static IReadOnlyList<BowlerId> Ids(int count)
        => [.. Enumerable.Range(0, count).Select(_ => BowlerId.New()).Order()];
}
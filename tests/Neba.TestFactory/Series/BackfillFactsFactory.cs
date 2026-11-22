using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class BackfillFactsFactory
{
    public static readonly DateOnly ValidAsOf = new(2025, 6, 28);

    /// <summary>Ready facts as of 6/28/2025 with no points.</summary>
    public static BackfillFacts Create(
        DateOnly? asOf = null,
        bool? ready = null,
        IReadOnlyList<BowlerPointsFact>? points = null)
        => new(asOf ?? ValidAsOf, ready ?? true, points ?? []);
}
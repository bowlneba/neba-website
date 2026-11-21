using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Steps the event job tests share: bring a saved Series to a state, then read it back through a new context.</summary>
internal static class AdvancementJobSupport
{
    /// <summary>Runs the advancement from a tournament with the facts the script describes, as an earlier event job would have.</summary>
    public static async Task AdvanceAsync(
        AdvancementRunHarness harness,
        AdvancementScenario scenario,
        AdvancementFactsScript script,
        Tournament from,
        CancellationToken cancellationToken)
    {
        var run = harness.CreateRun(FactsReaderDoubles.Reading(script.Build));

        var result = await run.Run.RunAsync(
            scenario.SeriesId,
            (series, facts, now) => series.Advance(from.Id, facts, now),
            AdvancementRunHarness.Wait,
            cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    /// <summary>Makes the Computed Qualifying and Survivor Stages Official, as the grace period sweep would.</summary>
    public static async Task MakeOfficialAsync(
        AdvancementRunHarness harness, AdvancementScenario scenario, CancellationToken cancellationToken)
    {
        var run = harness.CreateRun(FactsReaderDoubles.Untouched());

        var result = await run.Run.RunWithoutFactsAsync(
            scenario.SeriesId,
            (series, _, now) => series.MakeDueOfficial(now.AddDays(3), TimeSpan.FromHours(24)),
            AdvancementRunHarness.Wait,
            cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    public static SeriesStage StageOf(SeriesAggregate series, int order)
        => series.Stages.Single(stage => stage.Order == order);

    /// <summary>A Survivor Stage's stored output, in a form two reads can be compared by.</summary>
    public static List<(BowlerId Bowler, int? BlockTotal, bool Advanced)> OutcomesOf(SeriesStage stage)
        => [.. stage.SurvivorOutcomes
            .Select(outcome => (outcome.BowlerId, outcome.BlockTotal, outcome.Advanced))
            .OrderBy(outcome => outcome.BowlerId.Value)];
}
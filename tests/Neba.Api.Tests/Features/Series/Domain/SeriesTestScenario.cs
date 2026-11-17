using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

/// <summary>Builds Stage definitions and the tournament facts behind them, one tournament per Stage.</summary>
internal sealed class SeriesTestScenario(SeasonId? seasonId = null)
{
    public SeasonId SeasonId { get; } = seasonId ?? SeasonId.New();

    public List<SeriesStageDefinition> Stages { get; } = [];

    public Dictionary<TournamentId, StageTournamentFacts> Tournaments { get; } = [];

    public SeriesTestScenario Add(
        StageKind kind,
        int? count,
        DateOnly start,
        DateOnly? end = null,
        DateOnly? squadDate = null,
        TimeOnly? squadStart = null,
        string? name = null,
        SeasonId? season = null,
        string? otherSeries = null)
    {
        var facts = StageTournamentFactsFactory.Create(
            tournamentName: name ?? $"Tournament {Stages.Count + 1}",
            seasonId: season ?? SeasonId,
            startDate: start,
            endDate: end ?? start,
            otherSeriesName: otherSeries);

        Tournaments[facts.TournamentId] = facts;
        Stages.Add(SeriesStageDefinitionFactory.Create(
            kind: kind,
            tournamentId: facts.TournamentId,
            advancingCount: count,
            squadDate: squadDate,
            squadStartTime: squadStart));

        return this;
    }

    public SeriesTestScenario Qualifying(int month, int day, int count = 8, int? endDay = null)
        => Add(StageKind.Qualifying, count, new DateOnly(2025, month, day), endDay is null ? null : new DateOnly(2025, month, endDay.Value));

    public SeriesTestScenario Survivor(int month, int day, int count)
        => Add(StageKind.Survivor, count, new DateOnly(2025, month, day), squadDate: new DateOnly(2025, month, day), squadStart: new TimeOnly(9, 0));

    public SeriesTestScenario Placement(int month, int day)
        => Add(StageKind.Placement, null, new DateOnly(2025, month, day));

    public ErrorOr.ErrorOr<SeriesAggregate> Create(SeriesFinaleDefinition? finale = null)
        => SeriesAggregate.Create("Series", SeasonId, null, Stages, finale, Tournaments);
}

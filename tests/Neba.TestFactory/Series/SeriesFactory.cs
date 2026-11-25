using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.TestFactory.Series;

public static class SeriesFactory
{
    public const string ValidName = "Vincent Hall Memorial Series";

    /// <summary>No Stages by default (an incomplete Series is valid), so it persists with only a season.</summary>
    public static SeriesAggregate Create(
        string? name = null,
        SeasonId? seasonId = null,
        StoredFile? logo = null,
        IReadOnlyList<SeriesStageDefinition>? stages = null,
        SeriesFinaleDefinition? finale = null,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts>? tournaments = null)
    {
        var result = SeriesAggregate.Create(
            name ?? ValidName,
            seasonId ?? SeasonId.New(),
            logo,
            stages ?? [],
            finale,
            tournaments ?? new Dictionary<TournamentId, StageTournamentFacts>());

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create series: {result.Errors[0].Description}")
            : result.Value;
    }

    /// <summary>Builds a Series whose Stages run against the given tournaments, deriving the facts from them.</summary>
    public static SeriesAggregate CreateFor(
        Season season,
        IReadOnlyList<(Tournament Tournament, StageKind Kind, int? AdvancingCount, DateOnly? SquadDate, TimeOnly? SquadStartTime)> stages,
        SeriesFinaleDefinition? finale = null,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(season);
        ArgumentNullException.ThrowIfNull(stages);

        var facts = stages.ToDictionary(
            stage => stage.Tournament.Id,
            stage => StageTournamentFactsFactory.Create(
                tournamentId: stage.Tournament.Id,
                tournamentName: stage.Tournament.Name,
                seasonId: stage.Tournament.SeasonId,
                startDate: stage.Tournament.StartDate,
                endDate: stage.Tournament.EndDate));

        var definitions = stages
            .Select(stage => SeriesStageDefinitionFactory.Create(
                kind: stage.Kind,
                tournamentId: stage.Tournament.Id,
                advancingCount: stage.AdvancingCount,
                squadDate: stage.SquadDate,
                squadStartTime: stage.SquadStartTime))
            .ToList();

        return Create(name, season.Id, stages: definitions, finale: finale, tournaments: facts);
    }

    /// <summary>
    /// Builds a Series with the given Stage kinds (default one Qualifying Stage) and advances it through the domain, so every
    /// Stage is Computed with its stored output. Each Qualifying Stage advances 8 new bowlers; later Stages run over the
    /// whole field. Pass <paramref name="official"/> to make the Qualifying and Survivor Stages Official. Returns the facts
    /// used, so a test can advance the Series again.
    /// </summary>
    public static (SeriesAggregate Series, AdvancementFacts Facts) CreateWithAdvancement(
        IReadOnlyList<StageKind>? kinds = null,
        bool official = false,
        DateTimeOffset? now = null)
    {
        var at = now ?? new DateTimeOffset(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var stageKinds = kinds ?? [StageKind.Qualifying];
        var seasonId = SeasonId.New();
        var startDate = StageTournamentFactsFactory.ValidStartDate;

        var qualifiers = stageKinds.Select(kind => kind == StageKind.Qualifying
            ? Enumerable.Range(0, SeriesStageFactory.ValidAdvancingCount).Select(_ => BowlerId.New()).ToList()
            : []).ToList();
        var field = qualifiers.SelectMany(bowlers => bowlers).ToList();

        var definitions = new List<SeriesStageDefinition>();
        var facts = new Dictionary<TournamentId, StageTournamentFacts>();
        var tournaments = new Dictionary<TournamentId, StageTournamentResults>();
        var members = new Dictionary<DateOnly, IReadOnlySet<BowlerId>>();
        var survivorsSeen = 0;

        for (var index = 0; index < stageKinds.Count; index++)
        {
            var kind = stageKinds[index];
            var tournamentId = TournamentId.New();
            var start = startDate.AddDays(7 * index);
            var bowlers = kind == StageKind.Qualifying ? qualifiers[index] : field;

            int? count = null;
            if (kind == StageKind.Qualifying)
            {
                count = SeriesStageFactory.ValidAdvancingCount;
            }
            else if (kind == StageKind.Survivor)
            {
                count = 6 - (2 * survivorsSeen++);
            }

            definitions.Add(SeriesStageDefinitionFactory.Create(
                kind: kind,
                tournamentId: tournamentId,
                advancingCount: count,
                squadDate: kind == StageKind.Survivor ? start : null,
                squadStartTime: kind == StageKind.Survivor ? StageSquadFactFactory.ValidLocalStartTime : null));

            facts[tournamentId] = StageTournamentFactsFactory.Create(
                tournamentId: tournamentId, seasonId: seasonId, startDate: start);

            tournaments[tournamentId] = StageTournamentResultsFactory.Create(
                tournamentId: tournamentId,
                status: TournamentStatus.Completed,
                startDate: start,
                results: [.. bowlers.Select((bowler, place) => StageResultRowFactory.Create(bowler, place + 1))],
                squads: kind == StageKind.Survivor
                    ?
                    [
                        StageSquadFactFactory.Create(
                            localDate: start,
                            totals: [.. bowlers.Select((bowler, rank) => SquadBowlerTotalFactory.Create(bowler, 1500 - (rank * 10), 6))]),
                    ]
                    : []);

            members[start] = bowlers.ToHashSet();
        }

        var series = Create(seasonId: seasonId, stages: definitions, tournaments: facts);
        var advancementFacts = AdvancementFactsFactory.Create(tournaments, members);

        if (definitions.Count > 0)
        {
            _ = series.Advance(definitions[0].TournamentId, advancementFacts, at);
        }

        if (official)
        {
            _ = series.MakeReadyStagesOfficial(at.AddDays(2));
        }

        return (series, advancementFacts);
    }

    public static IReadOnlyCollection<SeriesAggregate> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: $"{faker.Company.CompanyName()} Series",
            seasonId: new SeasonId(Ulid.BogusString(faker))))];
    }
}
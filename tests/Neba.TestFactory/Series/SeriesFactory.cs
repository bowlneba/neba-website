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

    public static IReadOnlyCollection<SeriesAggregate> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: $"{faker.Company.CompanyName()} Series",
            seasonId: new SeasonId(Ulid.BogusString(faker))))];
    }
}
using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.TestFactory.Series;

public static class SeriesStageInputFactory
{
    public const string ValidKind = "Qualifying";
    public const string ValidTournamentId = "01JBQ5Y7K3M9N2P4R6T8V0W1X2";
    public const int ValidAdvancingCount = 8;

    /// <summary>A Qualifying Stage advancing 8. Pass a null count for a Placement Stage.</summary>
    public static SeriesStageInput Create(
        string? id = null,
        string? kind = null,
        string? tournamentId = null,
        int? advancingCount = ValidAdvancingCount,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null)
        => new()
        {
            Id = id,
            Kind = kind ?? ValidKind,
            TournamentId = tournamentId ?? ValidTournamentId,
            AdvancingCount = advancingCount,
            SquadDate = squadDate,
            SquadStartTime = squadStartTime,
        };

    public static IReadOnlyCollection<SeriesStageInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            tournamentId: Ulid.BogusString(faker),
            advancingCount: faker.Random.Int(1, 40)))];
    }
}
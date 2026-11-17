using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class TournamentSeriesDtoFactory
{
    public const string ValidName = "Vincent Hall Memorial Series";
    public const string ValidKind = "Qualifying";
    public const int ValidAdvancingCount = 8;

    /// <summary>A Qualifying Stage advancing 8, with no logo and no Finale.</summary>
    public static TournamentSeriesDto Create(
        SeriesId? seriesId = null,
        string? name = null,
        Uri? logoUrl = null,
        string? kind = null,
        int? advancingCount = ValidAdvancingCount,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null,
        int? finaleCount = null,
        DateOnly? finaleDate = null,
        TimeOnly? finaleStartTime = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            Name = name ?? ValidName,
            LogoUrl = logoUrl,
            Kind = kind ?? ValidKind,
            AdvancingCount = advancingCount,
            SquadDate = squadDate,
            SquadStartTime = squadStartTime,
            FinaleCount = finaleCount,
            FinaleDate = finaleDate,
            FinaleStartTime = finaleStartTime,
        };

    public static IReadOnlyCollection<TournamentSeriesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: $"{faker.Company.CompanyName()} Series",
            kind: faker.PickRandom("Qualifying", "Survivor"),
            advancingCount: faker.Random.Int(1, 40)))];
    }
}

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class SeasonWithStatsDtoFactory
{
    public const string ValidDescription = "2025 Season";
    public static readonly DateOnly ValidStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 12, 31);

    public static SeasonWithStatsDto Create(
        SeasonId? id = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
        => new()
        {
            Id = id ?? SeasonId.New(),
            Description = description ?? ValidDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
        };

    public static IReadOnlyCollection<SeasonWithStatsDto> Bogus(int count, int? seed = null)
    {
        var preFaker = seed.HasValue ? new Faker { Random = new Randomizer(seed.Value) } : new Faker();
        var currentYear = preFaker.Random.Int(2000, 2025 - count);

        var faker = new Faker<SeasonWithStatsDto>()
            .CustomInstantiator(f =>
            {
                var year = currentYear++;
                return new SeasonWithStatsDto
                {
                    Id = new SeasonId(Ulid.BogusString(f)),
                    Description = $"{year} Season",
                    StartDate = new DateOnly(year, 9, 1),
                    EndDate = new DateOnly(year + 1, 8, 31),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
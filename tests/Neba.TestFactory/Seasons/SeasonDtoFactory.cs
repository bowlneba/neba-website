using Bogus;

using Neba.Application.Seasons;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Seasons;

public static class SeasonDtoFactory
{
    public const string ValidDescription = "2025 Season";
    public static readonly DateOnly ValidStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 12, 31);

    public static SeasonDto Create(
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

    public static IReadOnlyCollection<SeasonDto> Bogus(int count, int? seed = null)
    {
        var preFaker = seed.HasValue ? new Faker { Random = new Randomizer(seed.Value) } : new Faker();
        var currentYear = preFaker.Random.Int(2000, 2025 - count);

        var faker = new Faker<SeasonDto>()
            .CustomInstantiator(f =>
            {
                var year = currentYear++;
                return new SeasonDto
                {
                    Id = new SeasonId(Ulid.BogusString(f)),
                    Description = $"{year} Season",
                    StartDate = new DateOnly(year, 9, 1),
                    EndDate = new DateOnly(year + 1, 8, 31),
                };
            });

        if (seed.HasValue)
            faker.UseSeed(seed.Value);

        return faker.Generate(count);
    }
}
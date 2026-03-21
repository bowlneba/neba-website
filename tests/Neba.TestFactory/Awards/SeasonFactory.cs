using Bogus;

using Neba.Domain.Awards;

namespace Neba.TestFactory.Awards;

public static class SeasonFactory
{
    public const string ValidDescription = "2025 Season";
    public static readonly DateOnly ValidStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 12, 31);

    public static Season Create(
        SeasonId? id = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool complete = false)
    {
        return new Season
        {
            Id = id ?? SeasonId.New(),
            Description = description ?? ValidDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            Complete = complete
        };
    }
    
    public static IReadOnlyCollection<Season> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<Season>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonId(Ulid.Bogus(f)),
                Description = f.Lorem.Sentence(3),
                StartDate = f.Date.PastDateOnly(5),
                EndDate = f.Date.FutureDateOnly(5),
                Complete = f.Random.Bool()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
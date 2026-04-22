using Bogus;

using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.TestFactory.Tournaments;

public static class SeasonViewModelFactory
{
    public const string ValidId = "01JBMS00000000000000000001";
    public const string ValidDescription = "2026 Season";
    public static readonly DateOnly ValidStartDate = new(2026, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2026, 12, 31);

    public static SeasonViewModel Create(
        string? id = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
        => new()
        {
            Id = id ?? ValidId,
            Description = description ?? ValidDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
        };

    public static IReadOnlyCollection<SeasonViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonViewModel>()
            .CustomInstantiator(f =>
            {
                var year = f.Date.Past(50).Year;

                return new SeasonViewModel
                {
                    Id = Ulid.BogusString(f),
                    Description = $"{year} Season",
                    StartDate = new(year, 1, 1),
                    EndDate = new(year, 12, 31)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

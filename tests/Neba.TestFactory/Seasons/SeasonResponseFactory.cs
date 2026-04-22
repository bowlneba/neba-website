using Bogus;

using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Seasons;

public static class SeasonResponseFactory
{
    public const string ValidDescription = "2025-2026 Season";
    public static readonly DateOnly ValidStartDate = new(2025, 9, 1);
    public static readonly DateOnly ValidEndDate = new(2026, 8, 31);

    public static SeasonResponse Create(
        string? id = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
        => new()
        {
            Id = id ?? SeasonId.New().Value.ToString(),
            Description = description ?? ValidDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
        };

    public static IReadOnlyCollection<SeasonResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonResponse>()
            .CustomInstantiator(f =>
            {
                var startDate = f.Date.PastDateOnly(50);
                return new SeasonResponse
                {
                    Id = Ulid.BogusString(f),
                    Description = $"{startDate.Year}-{startDate.Year + 1} Season",
                    StartDate = startDate,
                    EndDate = new DateOnly(startDate.Year + 1, startDate.Month, startDate.Day),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
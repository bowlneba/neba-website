using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailSeasonResponseFactory
{
    public const string ValidId = "01KNPMEYKAR8YHHZ0FSPX91MNN";
    public const string ValidDescription = "2024-2025 Season";
    public static readonly DateOnly ValidStartDate = new(2024, 9, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 8, 31);

    public static TournamentDetailSeasonResponse Create(
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

    public static IReadOnlyCollection<TournamentDetailSeasonResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailSeasonResponse>()
            .CustomInstantiator(f =>
            {
                var startDate = f.Date.PastDateOnly(50);
                return new TournamentDetailSeasonResponse
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
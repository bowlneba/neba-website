using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesTournamentOptionFactory
{
    public const string ValidId = "01JBQ5Y7K3M9N2P4R6T8V0X001";
    public const string ValidLabel = "Sat 5/31 · Westgate Lanes";
    public static readonly DateOnly ValidStartDate = new(2025, 5, 31);

    internal static SeriesTournamentOption Create(
        string? id = null,
        string? label = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int? teamSize = null)
        => new(
            id ?? ValidId,
            label ?? ValidLabel,
            startDate ?? ValidStartDate,
            endDate ?? startDate ?? ValidStartDate,
            teamSize ?? 1);

    internal static IReadOnlyCollection<SeriesTournamentOption> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var start = faker.Date.FutureDateOnly(1);
            return Create(
                id: Ulid.BogusString(faker),
                label: SeriesDisplay.TournamentOption(start, faker.Company.CompanyName()),
                startDate: start,
                teamSize: faker.Random.Int(1, 3));
        })];
    }
}
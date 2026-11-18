using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeasonOptionFactory
{
    public const string ValidId = "01JBMS00000000000000000001";
    public const string ValidLabel = "2025 Season (1/1/2025 to 12/31/2025)";
    public static readonly DateOnly ValidStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 12, 31);

    internal static SeasonOption Create(
        string? id = null,
        string? label = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
        => new(
            id ?? ValidId,
            label ?? ValidLabel,
            startDate ?? ValidStartDate,
            endDate ?? ValidEndDate);

    internal static IReadOnlyCollection<SeasonOption> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var year = 2000 + index;
            return Create(
                id: Ulid.BogusString(faker),
                label: year.ToString(System.Globalization.CultureInfo.InvariantCulture) + " Season",
                startDate: new DateOnly(year, 1, 1),
                endDate: new DateOnly(year, 12, 31));
        })];
    }
}

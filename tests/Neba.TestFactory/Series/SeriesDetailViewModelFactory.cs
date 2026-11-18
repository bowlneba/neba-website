using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesDetailViewModelFactory
{
    public const string ValidId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";
    public const string ValidName = "2025 Vincent Hall Memorial Series";
    public const string ValidSeasonId = "01JBMS00000000000000000001";
    public const string ValidSeasonDescription = "2025 Season";
    public const string ValidSeasonDateRange = "1/1/2025 to 12/31/2025";

    internal static SeriesDetailViewModel Create(
        string? id = null,
        string? name = null,
        string? seasonId = null,
        string? seasonDescription = null,
        string? seasonDateRange = null,
        bool? isLocked = null,
        SeriesFinaleViewModel? finale = null,
        Uri? logoUrl = null,
        SeriesLogoInput? logo = null,
        IReadOnlyList<SeriesStageViewModel>? stages = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            SeasonId = seasonId ?? ValidSeasonId,
            SeasonDescription = seasonDescription ?? ValidSeasonDescription,
            SeasonDateRange = seasonDateRange ?? ValidSeasonDateRange,
            IsLocked = isLocked ?? false,
            Finale = finale,
            LogoUrl = logoUrl,
            Logo = logo,
            Stages = stages ?? [SeriesStageViewModelFactory.Create()],
        };

    /// <summary>The 2025 reference configuration: 11 Stages (8 Qualifying, 2 Survivor, 1 Placement), with or without a Finale.</summary>
    internal static SeriesDetailViewModel Reference2025(bool withFinale = true)
    {
        // Name, start, end, kind, advancing count, team size, squad date.
        (string Name, DateOnly Start, DateOnly End, string Kind, int? Count, int TeamSize, DateOnly? Squad)[] rows =
        [
            ("Callahan's Bowl-O-Rama", new(2025, 1, 24), new(2025, 1, 25), StageKinds.Qualifying, 8, 1, null),
            ("Trios Team Event", new(2025, 2, 21), new(2025, 2, 21), StageKinds.Qualifying, 2, 3, null),
            ("NEBA Doubles, Baker Finals", new(2025, 2, 22), new(2025, 2, 22), StageKinds.Qualifying, 4, 2, null),
            ("Sparetime", new(2025, 3, 22), new(2025, 3, 22), StageKinds.Qualifying, 8, 1, null),
            ("BuddiesProShop.com Open", new(2025, 4, 26), new(2025, 4, 26), StageKinds.Qualifying, 8, 1, null),
            ("Westgate Lanes", new(2025, 5, 31), new(2025, 5, 31), StageKinds.Qualifying, 8, 1, null),
            ("Over 50 / Under Baker Doubles", new(2025, 6, 13), new(2025, 6, 13), StageKinds.Qualifying, 4, 2, null),
            ("Singles, Cranston", new(2025, 6, 14), new(2025, 6, 14), StageKinds.Qualifying, 8, 1, null),
            ("Callahan's Bowl-O-Rama Survivor", new(2025, 7, 12), new(2025, 7, 12), StageKinds.Survivor, 40, 1, new(2025, 7, 12)),
            ("Lang's Singles Open", new(2025, 8, 23), new(2025, 8, 23), StageKinds.Survivor, 20, 1, new(2025, 8, 23)),
            ("East Providence Singles", new(2025, 9, 13), new(2025, 9, 13), StageKinds.Placement, null, 1, null),
        ];

        return Create(
            finale: withFinale ? SeriesFinaleViewModelFactory.Create() : null,
            stages: [.. rows.Select((row, index) => SeriesStageViewModelFactory.Create(
                id: "01JBQ5Y7K3M9N2P4R6T8V0W" + (index + 10).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(3, '0'),
                order: index + 1,
                kind: row.Kind,
                advancingCount: row.Count,
                squadDate: row.Squad,
                squadStartTime: row.Squad is null ? null : new TimeOnly(9, 0),
                tournamentId: "01JBQ5Y7K3M9N2P4R6T8V0X" + (index + 10).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(3, '0'),
                tournamentName: row.Name,
                tournamentStartDate: row.Start,
                tournamentEndDate: row.End,
                teamSize: row.TeamSize))]);
    }

    internal static IReadOnlyCollection<SeriesDetailViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            id: Ulid.BogusString(faker),
            name: faker.Company.CompanyName() + " Series",
            seasonId: Ulid.BogusString(faker),
            stages: [.. SeriesStageViewModelFactory.Bogus(faker.Random.Int(1, 5), faker.Random.Int())]))];
    }
}
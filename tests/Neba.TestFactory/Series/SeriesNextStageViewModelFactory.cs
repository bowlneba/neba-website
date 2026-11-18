using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesNextStageViewModelFactory
{
    public const string ValidKind = "Survivor";
    public const string ValidTournamentName = "Callahan's Bowl-O-Rama Survivor";
    public const string ValidDates = "7/12";

    internal static SeriesNextStageViewModel Create(string? kind = null, string? tournamentName = null, string? dates = null)
        => new()
        {
            Kind = kind ?? ValidKind,
            TournamentName = tournamentName ?? ValidTournamentName,
            Dates = dates ?? ValidDates,
        };

    internal static IReadOnlyCollection<SeriesNextStageViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            kind: StageKinds.All[faker.Random.Int(0, StageKinds.All.Count - 1)],
            tournamentName: faker.Company.CompanyName(),
            dates: SeriesDisplay.TournamentDates(new DateOnly(2025, 7, 12), new DateOnly(2025, 7, 12))))];
    }
}

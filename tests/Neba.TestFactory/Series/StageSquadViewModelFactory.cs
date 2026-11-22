using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageSquadViewModelFactory
{
    public const string ValidSquadId = "01JBQ5Y7K3M9N2P4R6T8V0W4C1";
    public const string ValidLabel = "9:30 AM on 7/12";

    /// <summary>A squad with scores that the Stage does not use. The detail follows the flags unless given.</summary>
    internal static StageSquadViewModel Create(
        string? squadId = null,
        string? label = null,
        string? detail = null,
        bool hasScores = true,
        bool isSelected = false)
        => new(
            squadId ?? ValidSquadId,
            label ?? ValidLabel,
            detail ?? SeriesDisplay.SquadDetail(hasScores, isSelected),
            hasScores,
            isSelected);

    /// <summary>Squads on consecutive half hours; the first is selected, every second one has scores.</summary>
    internal static IReadOnlyList<StageSquadViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        var date = faker.Date.FutureDateOnly(1);
        return [.. Enumerable.Range(0, count).Select(index => Create(
            squadId: Ulid.BogusString(faker),
            label: SeriesDisplay.SquadOption(date, new TimeOnly(9, 0).AddMinutes(index * 30)),
            hasScores: index % 2 == 0,
            isSelected: index == 0))];
    }
}
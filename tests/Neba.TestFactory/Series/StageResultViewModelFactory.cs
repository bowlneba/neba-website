using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageResultViewModelFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPlace = 12;

    /// <summary>The label follows the name and place unless one is given.</summary>
    internal static StageResultViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? place = null,
        string? label = null)
    {
        var name = bowlerName ?? ValidBowlerName;
        var finish = place ?? ValidPlace;
        return new(bowlerId ?? ValidBowlerId, name, finish, label ?? SeriesDisplay.ResultOption(name, finish));
    }

    internal static IReadOnlyList<StageResultViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(index => Create(
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            place: index + 1))];
    }
}
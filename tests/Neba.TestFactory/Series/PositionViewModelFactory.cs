using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class PositionViewModelFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPosition = 1;

    /// <summary>First position, bowled, no Finale (finalist flag null).</summary>
    internal static PositionViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? position = null,
        bool bowled = true,
        bool? isFinalist = null)
        => new(bowlerId ?? ValidBowlerId, bowlerName ?? ValidBowlerName, position ?? ValidPosition, bowled, isFinalist);

    /// <summary>Positions 1..count, all with a Finale flag (the first four are finalists).</summary>
    internal static IReadOnlyList<PositionViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(index => Create(
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            position: index + 1,
            bowled: index % 6 != 5,
            isFinalist: index < 4))];
    }
}
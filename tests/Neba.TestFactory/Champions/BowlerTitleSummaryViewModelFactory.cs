using Neba.Api.Features.Bowlers.Domain;
using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class BowlerTitleSummaryViewModelFactory
{
    public const string ValidBowlerName = "Joe Bowler";
    public const string ValidBowlerLastName = "Bowler";
    public const string ValidBowlerFirstName = "Joe";
    public const int ValidTitleCount = 5;
    public const bool ValidHallOfFame = false;

    public static BowlerTitleSummaryViewModel Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        string? bowlerLastName = null,
        string? bowlerFirstName = null,
        int? titleCount = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            BowlerLastName = bowlerLastName ?? ValidBowlerLastName,
            BowlerFirstName = bowlerFirstName ?? ValidBowlerFirstName,
            TitleCount = titleCount ?? ValidTitleCount,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    internal static IReadOnlyCollection<BowlerTitleSummaryViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            return new BowlerTitleSummaryViewModel
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = $"{firstName} {lastName}",
                BowlerLastName = lastName,
                BowlerFirstName = firstName,
                TitleCount = faker.Random.Int(1, 30),
                HallOfFame = faker.Random.Bool(),
            };
        })];
    }

    public static IReadOnlyCollection<BowlerTitleSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
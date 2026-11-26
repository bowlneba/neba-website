using Bogus;

using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlersViewModelFactory
{
    public const bool ValidListApplies = true;
    public const int ValidMinTournamentsCount = 3;

    public static EligibleBowlersViewModel Create(
        bool? listApplies = null,
        int? minTournamentsCount = ValidMinTournamentsCount,
        IReadOnlyList<EligibleBowlerViewModel>? bowlers = null)
        => new()
        {
            ListApplies = listApplies ?? ValidListApplies,
            MinTournamentsCount = minTournamentsCount,
            Bowlers = bowlers ?? [EligibleBowlerViewModelFactory.Create()]
        };

    public static EligibleBowlersViewModel CreateNotApplicable()
        => Create(listApplies: false, minTournamentsCount: null, bowlers: []);

    internal static IReadOnlyCollection<EligibleBowlersViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlers: EligibleBowlerViewModelFactory.Bogus(faker.Random.Int(1, 5), faker)))];
    }

    public static IReadOnlyCollection<EligibleBowlersViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}

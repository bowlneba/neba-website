using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

internal static class TeamMemberFactsFactory
{
    public static readonly DateOnly ValidDateOfBirth = new(1990, 6, 15);

    public static TeamMemberFacts Create(
        BowlerId? bowlerId = null,
        DateOnly? dateOfBirth = null,
        IReadOnlyCollection<NebaTitle>? titles = null,
        bool hallOfFameSuperiorPerformance = false)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            DateOfBirth = dateOfBirth ?? ValidDateOfBirth,
            Titles = titles ?? [],
            HallOfFameSuperiorPerformance = hallOfFameSuperiorPerformance
        };

    /// <summary>A bowler who is not in the system: no ID, no date of birth, no titles.</summary>
    public static TeamMemberFacts Unknown() => new();

    public static IReadOnlyList<TeamMemberFacts> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => new TeamMemberFacts
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            DateOfBirth = faker.Date.PastDateOnly(faker.Random.Int(20, 80)),
            Titles = [],
            HallOfFameSuperiorPerformance = false
        })];
    }
}

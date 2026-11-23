using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class StageSquadFactFactory
{
    public static readonly DateOnly ValidLocalDate = new(2025, 7, 12);
    public static readonly TimeOnly ValidLocalStartTime = new(9, 0);

    /// <summary>A 9:00 AM squad on 7/12/2025 with no scores.</summary>
    public static StageSquadFact Create(
        SquadId? squadId = null,
        DateOnly? localDate = null,
        TimeOnly? localStartTime = null,
        IReadOnlyList<SquadBowlerTotal>? totals = null)
        => new(
            squadId ?? SquadId.New(),
            localDate ?? ValidLocalDate,
            localStartTime ?? ValidLocalStartTime,
            totals ?? []);

    public static IReadOnlyList<StageSquadFact> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new SquadId(Ulid.BogusString(faker)),
            faker.Date.FutureDateOnly(1),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(faker.Random.Int(8, 17))),
            SquadBowlerTotalFactory.Bogus(faker.Random.Int(1, 5), faker.Random.Int())))];
    }
}
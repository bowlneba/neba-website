using Bogus;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class SquadScoreFactory
{
    public const short ValidGameNumber = 1;
    public const int ValidValue = 200;

    public static SquadScore Create(
        SquadId? squadId = null,
        BowlerId? bowlerId = null,
        short? gameNumber = null,
        int? value = null)
    {
        var result = SquadScore.Create(
            squadId: squadId ?? SquadId.New(),
            bowlerId: bowlerId ?? BowlerId.New(),
            gameNumber: gameNumber ?? ValidGameNumber,
            score: value ?? ValidValue);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create squad score: {result.Errors[0].Description}")
            : result.Value;
    }

    internal static IReadOnlyCollection<SquadScore> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(i =>
        {
            var result = SquadScore.Create(
                squadId: new SquadId(Ulid.BogusString(faker)),
                bowlerId: new BowlerId(Ulid.BogusString(faker)),
                gameNumber: (short)(i + 1),
                score: faker.Random.Int(0, 300));

            return result.IsError
                ? throw new InvalidOperationException($"Failed to create squad score: {result.Errors[0].Description}")
                : result.Value;

        })];
    }

    public static IReadOnlyCollection<SquadScore> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
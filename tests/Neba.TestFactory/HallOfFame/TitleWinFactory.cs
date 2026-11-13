using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.HallOfFame;

internal static class TitleWinFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";
    public const string ValidSeasonId = "01JZ8K3M4N5P6Q7R8S9T0V1W3A";

    public static readonly DateOnly ValidStartDate = new(2019, 3, 9);
    public static readonly DateOnly ValidEndDate = new(2019, 3, 10);

    public static TitleWin Create(
        BowlerId? bowlerId = null,
        SeasonId? seasonId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TournamentType? tournamentType = null)
        => new(
            bowlerId ?? new BowlerId(ValidBowlerId),
            seasonId ?? new SeasonId(ValidSeasonId),
            startDate ?? ValidStartDate,
            endDate ?? ValidEndDate,
            tournamentType ?? TournamentType.Singles);

    public static IReadOnlyCollection<TitleWin> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var start = faker.Date.PastDateOnly(refDate: new DateOnly(2026, 1, 1));
            return new TitleWin(
                new BowlerId(Ulid.BogusString(faker)),
                new SeasonId(Ulid.BogusString(faker)),
                start,
                start.AddDays(1),
                faker.PickRandom(TournamentType.List.ToArray()));
        })];
    }
}
using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Bowlers;

public static class BowlerTitleDtoFactory
{
    public const string ValidTournamentName = "Singles Classic";
    public static readonly DateOnly ValidTournamentDate = new(2024, 11, 1);
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static BowlerTitleDto Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        TournamentType? tournamentType = null)
        => new()
        {
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
        };

    internal static IReadOnlyCollection<BowlerTitleDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitleDto
        {
            TournamentId = new TournamentId(Ulid.BogusString(faker)),
            TournamentName = faker.Random.Words(2),
            TournamentDate = faker.Date.PastDateOnly(10),
            TournamentType = faker.PickRandom(TournamentType.List.ToArray()).Name,
        })];
    }

    public static IReadOnlyCollection<BowlerTitleDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
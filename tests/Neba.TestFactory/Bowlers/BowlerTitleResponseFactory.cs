using Neba.Api.Contracts.Bowlers.GetBowlerTitles;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Bowlers;

public static class BowlerTitleResponseFactory
{
    public const string ValidTournamentName = "Singles Classic";
    public static readonly DateOnly ValidTournamentDate = new(2024, 11, 1);
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static BowlerTitleResponse Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        TournamentType? tournamentType = null)
        => new()
        {
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
        };

    public static IReadOnlyCollection<BowlerTitleResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitleResponse
        {
            TournamentId = Ulid.BogusString(faker),
            TournamentName = faker.Random.Words(2),
            TournamentDate = faker.Date.PastDateOnly(10),
            TournamentType = faker.PickRandom(TournamentType.List.ToArray()).Name,
        })];
    }

    public static IReadOnlyCollection<BowlerTitleResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
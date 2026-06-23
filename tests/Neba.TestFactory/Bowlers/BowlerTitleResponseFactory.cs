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

    public static IReadOnlyCollection<BowlerTitleResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitleResponse>()
            .CustomInstantiator(f => new BowlerTitleResponse
            {
                TournamentId = Ulid.BogusString(f),
                TournamentName = f.Random.Words(2),
                TournamentDate = f.Date.PastDateOnly(10),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<BowlerTitleResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}

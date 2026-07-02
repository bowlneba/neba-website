using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TournamentChampionResponseFactory
{
    public const string ValidTournamentName = "Singles Classic";
    public static readonly DateOnly ValidTournamentDate = new(2024, 11, 1);
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TournamentChampionResponse Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        TournamentType? tournamentType = null,
        IReadOnlyCollection<ChampionResponse>? champions = null)
        => new()
        {
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
            Champions = champions ?? [ChampionResponseFactory.Create()],
        };

    public static IReadOnlyCollection<TournamentChampionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentChampionResponse>()
            .CustomInstantiator(f => new TournamentChampionResponse
            {
                TournamentId = Ulid.BogusString(f),
                TournamentName = f.Random.Words(2),
                TournamentDate = f.Date.PastDateOnly(10),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
                Champions = ChampionResponseFactory.Bogus(f.Random.Int(1, 4), f),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TournamentChampionResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
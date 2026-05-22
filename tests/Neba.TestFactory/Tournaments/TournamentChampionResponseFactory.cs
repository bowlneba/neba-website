using Bogus;

using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TournamentChampionResponseFactory
{
    public const string ValidBowlerName = "Jane Smith";
    public const bool ValidHallOfFame = false;
    public const string ValidTournamentName = "Singles Classic";
    public const string ValidTournamentDate = "November 1, 2024";
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TournamentChampionResponse Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        bool? hallOfFame = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        string? tournamentDate = null,
        TournamentType? tournamentType = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
        };

    public static IReadOnlyCollection<TournamentChampionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentChampionResponse>()
            .CustomInstantiator(f => new TournamentChampionResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                HallOfFame = f.Random.Bool(),
                TournamentId = Ulid.BogusString(f),
                TournamentName = f.Random.Words(2),
                TournamentDate = f.Date.PastDateOnly(10).ToString("MMMM d, yyyy"),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

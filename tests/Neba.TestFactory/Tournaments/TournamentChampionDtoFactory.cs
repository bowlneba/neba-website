using Bogus;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListChampions;

namespace Neba.TestFactory.Tournaments;

public static class TournamentChampionDtoFactory
{
    public const string ValidTournamentName = "Singles Classic";
    public static readonly DateOnly ValidTournamentDate = new(2024, 11, 1);
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TournamentChampionDto Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        TournamentType? tournamentType = null,
        IReadOnlyCollection<ChampionDto>? champions = null)
        => new()
        {
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = tournamentType ?? ValidTournamentType,
            Champions = champions ?? [ChampionDtoFactory.Create()],
        };

    public static IReadOnlyCollection<TournamentChampionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentChampionDto>()
            .CustomInstantiator(f => new TournamentChampionDto
            {
                TournamentId = new TournamentId(Ulid.BogusString(f)),
                TournamentName = f.Random.Words(2),
                TournamentDate = f.Date.PastDateOnly(10),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()),
                Champions = ChampionDtoFactory.Bogus(f.Random.Int(1, 4), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

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

    public static IReadOnlyCollection<BowlerTitleDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitleDto>()
            .CustomInstantiator(f => new BowlerTitleDto
            {
                TournamentId = new TournamentId(Ulid.BogusString(f)),
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

    public static IReadOnlyCollection<BowlerTitleDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
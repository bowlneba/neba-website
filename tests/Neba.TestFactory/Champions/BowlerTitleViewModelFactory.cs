using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class BowlerTitleViewModelFactory
{
    public const string ValidBowlerName = "Joe Bowler";
    public const int ValidTournamentMonth = 4;
    public const int ValidTournamentYear = 2024;
    public const bool ValidHallOfFame = false;

    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static BowlerTitleViewModel Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        TournamentId? tournamentId = null,
        int? tournamentMonth = null,
        int? tournamentYear = null,
        TournamentType? tournamentType = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            TournamentMonth = tournamentMonth ?? ValidTournamentMonth,
            TournamentYear = tournamentYear ?? ValidTournamentYear,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    public static IReadOnlyCollection<BowlerTitleViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitleViewModel>()
            .CustomInstantiator(f =>
            {
                var date = f.Date.PastDateOnly(10);
                return new BowlerTitleViewModel
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    TournamentId = Ulid.BogusString(f),
                    TournamentMonth = date.Month,
                    TournamentYear = date.Year,
                    TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
                    HallOfFame = f.Random.Bool(),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
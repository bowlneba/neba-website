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

    public static IReadOnlyCollection<BowlerTitleViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var date = faker.Date.PastDateOnly(10);
            return new BowlerTitleViewModel
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                TournamentId = Ulid.BogusString(faker),
                TournamentMonth = date.Month,
                TournamentYear = date.Year,
                TournamentType = faker.PickRandom(TournamentType.List.ToArray()).Name,
                HallOfFame = faker.Random.Bool(),
            };
        })];
    }

    public static IReadOnlyCollection<BowlerTitleViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
using System.Globalization;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class TitleViewModelFactory
{
    public const string ValidTournamentName = "Singles Classic";
    public const string ValidTournamentDate = "Apr 2024";

    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TitleViewModel Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        string? tournamentDate = null,
        TournamentType? tournamentType = null)
        => new()
        {
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentType = (tournamentType ?? ValidTournamentType).Name,
        };

    public static IReadOnlyCollection<TitleViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TitleViewModel>()
            .CustomInstantiator(f =>
            {
                var date = f.Date.PastDateOnly(10);
                return new TitleViewModel
                {
                    TournamentId = Ulid.BogusString(f),
                    TournamentName = f.Random.Words(2),
                    TournamentDate = date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TitleViewModel> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
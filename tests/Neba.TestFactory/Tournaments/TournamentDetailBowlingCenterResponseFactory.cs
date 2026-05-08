using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailBowlingCenterResponseFactory
{
    public const string ValidName = "Acme Lanes";
    public const string ValidCity = "Springfield";
    public const string ValidState = "MA";

    public static TournamentDetailBowlingCenterResponse Create(
        string? name = null,
        string? city = null,
        string? state = null)
        => new()
        {
            Name = name ?? ValidName,
            City = city ?? ValidCity,
            State = state ?? ValidState,
        };

    public static IReadOnlyCollection<TournamentDetailBowlingCenterResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailBowlingCenterResponse>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName() + " Lanes",
                City = f.Address.City(),
                State = f.Address.StateAbbr(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
using Neba.Api.Features.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailBowlingCenterDtoFactory
{
    public const string ValidName = "Test Lanes";
    public const string ValidCity = "Springfield";
    public const string ValidState = "IL";

    public static TournamentDetailBowlingCenterDto Create(
        string? name = null,
        string? city = null,
        string? state = null)
        => new()
        {
            Name = name ?? ValidName,
            City = city ?? ValidCity,
            State = state ?? ValidState,
        };

    public static IReadOnlyCollection<TournamentDetailBowlingCenterDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailBowlingCenterDto>()
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

using Neba.Api.Features.Tournaments.ListTournamentsInSeason;

namespace Neba.TestFactory.Tournaments;

public static class SeasonTournamentSponsorDtoFactory
{
    public const string ValidName = "Test Sponsor";
    public const string ValidSlug = "test-sponsor";

    public static SeasonTournamentSponsorDto Create(
        string? name = null,
        string? slug = null,
        Uri? logoUrl = null)
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            LogoUrl = logoUrl,
        };

    public static IReadOnlyCollection<SeasonTournamentSponsorDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonTournamentSponsorDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Internet.DomainWord(),
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;

namespace Neba.TestFactory.Tournaments;

public static class SeasonTournamentSponsorDtoFactory
{
    public const string ValidName = "Test Sponsor";
    public const string ValidSlug = "test-sponsor";

    public static SeasonTournamentSponsorDto Create(
        string? name = null,
        string? slug = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null)
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            LogoUrl = logoUrl,
            LogoContainer = logoContainer,
            LogoPath = logoPath,
        };

    public static IReadOnlyCollection<SeasonTournamentSponsorDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonTournamentSponsorDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Internet.DomainWord(),
                LogoUrl = null,
                LogoContainer = f.Random.Bool() ? f.Lorem.Word() : null,
                LogoPath = f.Random.Bool() ? f.System.FilePath() : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailSponsorResponseFactory
{
    public const string ValidName = "Acme Corp";
    public const string ValidSlug = "acme-corp";
    public const string ValidSponsorId = "01000000000000000000000001";

    public static TournamentDetailSponsorResponse Create(
        string? name = null,
        string? slug = null,
        Uri? logoUrl = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? sponsorId = null,
        bool? titleSponsor = null,
        decimal? sponsorshipAmount = null)
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            LogoUrl = logoUrl,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
            SponsorId = sponsorId ?? ValidSponsorId,
            TitleSponsor = titleSponsor ?? false,
            SponsorshipAmount = sponsorshipAmount ?? 500m,
        };

    public static IReadOnlyCollection<TournamentDetailSponsorResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailSponsorResponse>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Avatar()) : null,
                WebsiteUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                TagPhrase = f.Random.Bool() ? f.Company.CatchPhrase() : null,
                SponsorId = Ulid.BogusString(f),
                TitleSponsor = f.Random.Bool(),
                SponsorshipAmount = f.Finance.Amount(0, 5000),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TournamentDetailSponsorResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
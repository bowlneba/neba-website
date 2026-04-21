using Bogus;

using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.TestFactory.Tournaments;

public static class TournamentSummaryViewModelFactory
{
    public const string ValidId = "2026-01-singles";
    public const string ValidName = "Granite State Open";
    public const string ValidSeason = "2026";
    public const string ValidPatternName = "Shark";

    public static readonly DateOnly ValidStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
    public static readonly DateOnly ValidEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;
    public static readonly TournamentEligibility ValidEligibility = TournamentEligibility.Open;

    public static TournamentSummaryViewModel Create(
        string? id = null,
        string? name = null,
        string? season = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TournamentType? tournamentType = null,
        TournamentEligibility? eligibility = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            Season = season ?? ValidSeason,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            TournamentType = tournamentType ?? ValidTournamentType,
            Eligibility = eligibility ?? ValidEligibility,
            EntryFee = 95m,
            RegistrationStatus = RegistrationStatus.Open,
            RegistrationUrl = new Uri("https://www.bowlneba.com/register", UriKind.Absolute),
            BowlingCenterName = "Striker Lanes",
            BowlingCenterCity = "Manchester, NH",
            Sponsor = "Acme Bowling",
            TournamentLogoUrl = new Uri("https://cdn.bowlneba.com/logos/granite-state-open.png", UriKind.Absolute),
            AddedMoney = 1500m,
            Entries = 52,
            MaxEntries = 80,
            PatternName = ValidPatternName,
            PatternLength = 43,
            PatternLengthCategory = "Medium",
            Winners = ["Alex Example"],
        };

    public static IReadOnlyCollection<TournamentSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentSummaryViewModel>()
            .CustomInstantiator(f =>
            {
                var start = f.Date.BetweenDateOnly(DateOnly.FromDateTime(DateTime.Today.AddDays(-180)), DateOnly.FromDateTime(DateTime.Today.AddDays(180)));
                var end = start.AddDays(f.Random.Int(0, 2));
                var entries = f.Random.Int(20, 120);
                var maxEntries = entries + f.Random.Int(0, 40);

                return new TournamentSummaryViewModel
                {
                    Id = f.Random.ReplaceNumbers("####-##-???"),
                    Name = f.Company.CompanyName() + " Open",
                    Season = f.Random.Int(2019, 2030).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    StartDate = start,
                    EndDate = end,
                    TournamentType = f.PickRandom<TournamentType>(),
                    Eligibility = f.PickRandom<TournamentEligibility>(),
                    EntryFee = f.Random.Decimal(60, 180),
                    RegistrationStatus = f.PickRandom<RegistrationStatus>(),
                    RegistrationUrl = new Uri(f.Internet.UrlWithPath(), UriKind.Absolute),
                    BowlingCenterName = f.Company.CompanyName() + " Lanes",
                    BowlingCenterCity = f.Address.City(),
                    Sponsor = f.Company.CompanyName(),
                    TournamentLogoUrl = new Uri(f.Internet.UrlWithPath(), UriKind.Absolute),
                    AddedMoney = f.Random.Decimal(0, 5000),
                    Entries = entries,
                    MaxEntries = maxEntries,
                    PatternName = f.Random.Word(),
                    PatternLength = f.Random.Int(35, 52),
                    PatternLengthCategory = f.PickRandom("Short", "Medium", "Long"),
                    Winners = [f.Name.FullName(), f.Name.FullName()],
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

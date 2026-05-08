using System.Globalization;

using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailViewModelFactory
{
    public const string ValidId = "01JSTX1234567890ABCDEFGHIJ";
    public const string ValidName = "Spring Open";
    public const string ValidSeasonDescription = "2025-2026 Season";
    public const string ValidTournamentType = "Singles";

    public static readonly DateOnly ValidStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public static readonly DateOnly ValidEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    public static TournamentDetailViewModel Create(
        string? id = null,
        string? name = null,
        string? seasonDescription = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? tournamentType = null,
        bool? statsEligible = null,
        decimal? entryFee = null,
        Uri? registrationUrl = null,
        decimal? addedMoney = null,
        int? entryCount = null,
        string? patternLengthCategory = null,
        Uri? logoUrl = null,
        string? bowlingCenterName = null,
        string? bowlingCenterCity = null,
        string? bowlingCenterState = null,
        IReadOnlyCollection<TournamentDetailSponsorViewModel>? sponsors = null,
        IReadOnlyCollection<TournamentDetailOilPatternViewModel>? oilPatterns = null,
        IReadOnlyCollection<string>? winners = null,
        IReadOnlyCollection<TournamentResultViewModel>? results = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            SeasonDescription = seasonDescription ?? ValidSeasonDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            TournamentType = tournamentType ?? ValidTournamentType,
            StatsEligible = statsEligible ?? true,
            EntryFee = entryFee,
            RegistrationUrl = registrationUrl,
            AddedMoney = addedMoney,
            EntryCount = entryCount,
            PatternLengthCategory = patternLengthCategory,
            LogoUrl = logoUrl,
            BowlingCenterName = bowlingCenterName,
            BowlingCenterCity = bowlingCenterCity,
            BowlingCenterState = bowlingCenterState,
            Sponsors = sponsors ?? [],
            OilPatterns = oilPatterns ?? [],
            Winners = winners ?? [],
            Results = results ?? [],
        };

    public static IReadOnlyCollection<TournamentDetailViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailViewModel>()
            .CustomInstantiator(f =>
            {
                var start = f.Date.PastDateOnly();
                return new TournamentDetailViewModel
                {
                    Id = Ulid.BogusString(f),
                    Name = f.Random.Words(3),
                    SeasonDescription = f.Random.Int(2020, 2030).ToString(CultureInfo.InvariantCulture) + " Season",
                    StartDate = start,
                    EndDate = start.AddDays(f.Random.Int(0, 2)),
                    TournamentType = f.PickRandom("Singles", "Doubles", "Trios"),
                    StatsEligible = f.Random.Bool(),
                    EntryFee = f.Random.Decimal(60, 180),
                    RegistrationUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                    AddedMoney = f.Random.Decimal(0, 5000),
                    EntryCount = f.Random.Int(10, 120),
                    PatternLengthCategory = f.PickRandom("Short", "Medium", "Long"),
                    LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                    BowlingCenterName = f.Company.CompanyName() + " Lanes",
                    BowlingCenterCity = f.Address.City(),
                    BowlingCenterState = f.Address.StateAbbr(),
                    Sponsors = TournamentDetailSponsorViewModelFactory.Bogus(f.Random.Int(0, 2), seed),
                    OilPatterns = TournamentDetailOilPatternViewModelFactory.Bogus(f.Random.Int(0, 2), seed),
                    Winners = [f.Name.FullName()],
                    Results = TournamentResultViewModelFactory.Bogus(f.Random.Int(0, 10), seed),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
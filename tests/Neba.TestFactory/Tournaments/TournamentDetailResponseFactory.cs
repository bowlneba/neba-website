using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailResponseFactory
{
    public const string ValidId = "01JSTX1234567890ABCDEFGHIJ";
    public const string ValidName = "Spring Open";
    public const string ValidSeason = "2024-2025 Season";

    public static TournamentDetailResponse Create(
        string? id = null,
        string? name = null,
        string? season = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? statsEligible = null,
        TournamentType? tournamentType = null,
        decimal? entryFee = null,
        Uri? registrationUrl = null,
        decimal? addedMoney = null,
        int? reservations = null,
        int? entryCount = null,
        string? patternLengthCategory = null,
        string? patternRatioCategory = null,
        Uri? logoUrl = null,
        TournamentDetailBowlingCenterResponse? bowlingCenter = null,
        IReadOnlyCollection<TournamentDetailSponsorResponse>? sponsors = null,
        IReadOnlyCollection<TournamentDetailOilPatternResponse>? oilPatterns = null,
        IReadOnlyCollection<string>? winners = null,
        IReadOnlyCollection<TournamentResultResponse>? results = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            Season = season ?? ValidSeason,
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today),
            StatsEligible = statsEligible ?? true,
            TournamentType = tournamentType?.Name ?? TournamentType.Singles.Name,
            EntryFee = entryFee,
            RegistrationUrl = registrationUrl,
            AddedMoney = addedMoney,
            Reservations = reservations,
            EntryCount = entryCount,
            PatternLengthCategory = patternLengthCategory,
            PatternRatioCategory = patternRatioCategory,
            LogoUrl = logoUrl,
            BowlingCenter = bowlingCenter,
            Sponsors = sponsors ?? [],
            OilPatterns = oilPatterns ?? [],
            Winners = winners ?? [],
            Results = results ?? [],
        };

    public static IReadOnlyCollection<TournamentDetailResponse> Bogus(int count, int? seed = null)
    {
        var bowlingCenters = TournamentDetailBowlingCenterResponseFactory.Bogus(10, seed).ToArray();
        var sponsors = TournamentDetailSponsorResponseFactory.Bogus(25, seed).ToArray();
        var oilPatterns = TournamentDetailOilPatternResponseFactory.Bogus(20, seed).ToArray();
        var winners = NameFactory.Bogus(count * 200, seed).ToArray();

        var faker = new Faker<TournamentDetailResponse>()
            .CustomInstantiator(f => new()
            {
                Id = Ulid.BogusString(f),
                Name = f.Random.Words(3),
                Season = $"{f.Date.Past(10).Year}-{f.Date.Past(10).Year + 1} Season",
                StartDate = DateOnly.FromDateTime(f.Date.Future()),
                EndDate = DateOnly.FromDateTime(f.Date.Future()),
                StatsEligible = f.Random.Bool(),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
                EntryFee = f.Random.Decimal(50, 200),
                RegistrationUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                AddedMoney = f.Random.Decimal(0, 5000),
                Reservations = f.Random.Int(0, 100),
                EntryCount = f.Random.Int(0, 100),
                PatternLengthCategory = f.PickRandom(PatternLengthCategory.List.ToArray())?.Name,
                PatternRatioCategory = f.PickRandom(PatternRatioCategory.List.ToArray())?.Name,
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                BowlingCenter = f.Random.Bool() ? f.PickRandom(bowlingCenters) : null,
                Sponsors = [.. f.PickRandom(sponsors, f.Random.Int(0, 2))],
                OilPatterns = [.. f.PickRandom(oilPatterns, f.Random.Int(0, 2))],
                Winners = [.. f.PickRandom(winners, f.Random.Int(0, 3)).Select(w => w.ToDisplayName())],
                Results = TournamentResultResponseFactory.Bogus(f.Random.Int(0, 10), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailDtoFactory
{
    public const string ValidSeason = "2024-2025 Season";

    public static TournamentDetailDto Create(
        TournamentId? id = null,
        string? name = null,
        string? season = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? statsEligible = null,
        TournamentType? tournamentType = null,
        decimal? entryFee = null,
        Uri? registrationUrl = null,
        TournamentDetailBowlingCenterDto? bowlingCenter = null,
        IReadOnlyCollection<TournamentDetailSponsorDto>? sponsors = null,
        decimal? addedMoney = null,
        int? reservations = null,
        PatternLengthCategory? patternLengthCategory = null,
        PatternRatioCategory? patternRatioCategory = null,
        IReadOnlyCollection<TournamentDetailOilPatternDto>? oilPatterns = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null,
        IReadOnlyCollection<Name>? winners = null,
        IReadOnlyCollection<TournamentResultDto>? results = null,
        int? entryCount = null)
            => new()
            {
                Id = id ?? TournamentId.New(),
                Name = name ?? "Test Tournament",
                Season = season ?? ValidSeason,
                StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Today),
                EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today),
                StatsEligible = statsEligible ?? true,
                TournamentType = tournamentType?.Name ?? TournamentType.Singles.Name,
                EntryFee = entryFee,
                RegistrationUrl = registrationUrl ?? new Uri("https://example.com/register"),
                BowlingCenter = bowlingCenter,
                Sponsors = sponsors ?? [],
                AddedMoney = addedMoney,
                Reservations = reservations,
                PatternLengthCategory = patternLengthCategory?.Name,
                PatternRatioCategory = patternRatioCategory?.Name,
                OilPatterns = oilPatterns ?? [],
                LogoUrl = logoUrl,
                LogoContainer = logoContainer,
                LogoPath = logoPath,
                Winners = winners ?? [],
                Results = results ?? [],
                EntryCount = entryCount
            };

    public static IReadOnlyCollection<TournamentDetailDto> Bogus(int count, int? seed = null)
    {
        var winners = NameFactory.Bogus(count * 200, seed).ToArray();
        var bowlingCenters = TournamentDetailBowlingCenterDtoFactory.Bogus(10, seed).ToArray();
        var sponsors = TournamentDetailSponsorDtoFactory.Bogus(25, seed).ToArray();
        var oilPatterns = TournamentDetailOilPatternDtoFactory.Bogus(20, seed).ToArray();

        var faker = new Faker<TournamentDetailDto>()
            .CustomInstantiator(f => new()
            {
                Id = new TournamentId(Ulid.BogusString(f)),
                Name = f.Random.Words(3),
                Season = $"{f.Date.Past(10).Year} Season",
                StartDate = DateOnly.FromDateTime(f.Date.Future()),
                EndDate = DateOnly.FromDateTime(f.Date.Future()),
                StatsEligible = f.Random.Bool(),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()).Name,
                EntryFee = f.Random.Decimal(50, 200),
                RegistrationUrl = new Uri(f.Internet.Url()),
                BowlingCenter = f.PickRandom(bowlingCenters),
                Sponsors = [.. f.PickRandom(sponsors, f.Random.Int(0, 2))],
                AddedMoney = f.Random.Decimal(0, 5000),
                Reservations = f.Random.Int(0, 100),
                PatternLengthCategory = f.PickRandom(PatternLengthCategory.List.ToArray())?.Name,
                PatternRatioCategory = f.PickRandom(PatternRatioCategory.List.ToArray())?.Name,
                OilPatterns = [.. f.PickRandom(oilPatterns, f.Random.Int(0, 2))],
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                LogoContainer = f.Random.Bool() ? f.System.FilePath() : null,
                LogoPath = f.Random.Bool() ? f.System.FilePath() : null,
                Winners = [.. f.PickRandom(winners, f.Random.Int(0, 3))],
                Results = TournamentResultDtoFactory.Bogus(f.Random.Int(0, 10), seed),
                EntryCount = f.Random.Int(0, 100)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}

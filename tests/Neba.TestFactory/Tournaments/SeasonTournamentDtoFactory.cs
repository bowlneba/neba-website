using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Seasons;
using Neba.Application.Sponsors;
using Neba.Application.Tournaments;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;

namespace Neba.TestFactory.Tournaments;

public static class SeasonTournamentDtoFactory
{
    public static SeasonTournamentDto Create(
        TournamentId? id = null,
        string? name = null,
        SeasonDto? season = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TournamentType? tournamentType = null,
        decimal? entryFee = null,
        Uri? registrationUrl = null,
        BowlingCenterSummaryDto? bowlingCenter = null,
        IReadOnlyCollection<SponsorSummaryDto>? sponsors = null,
        decimal? addedMoney = null,
        int? reservations = null,
        PatternLengthCategory? patternLengthCategory = null,
        PatternRatioCategory? patternRatioCategory = null,
        IReadOnlyCollection<TournamentOilPatternDto>? oilPatterns = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null,
        IReadOnlyCollection<Name>? winners = null)
            => new()
            {
                Id = id ?? TournamentId.New(),
                Name = name ?? "Test Tournament",
                Season = season ?? SeasonDtoFactory.Create(),
                StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Today),
                EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today),
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
                Winners = winners ?? []
            };

    public static IReadOnlyCollection<SeasonTournamentDto> Bogus(int count, int? seed = null)
    {
        var seasons = SeasonDtoFactory.Bogus(5, seed).ToArray();
        var winners = NameFactory.Bogus(count * 200, seed).ToArray();
        var bowlingCenters = BowlingCenterSummaryDtoFactory.Bogus(10, seed).ToArray();
        var sponsors = SponsorSummaryDtoFactory.Bogus(25, seed).ToArray();
        var oilPatterns = TournamentOilPatternDtoFactory.Bogus(20, seed).ToArray();

        var faker = new Faker<SeasonTournamentDto>()
            .CustomInstantiator(f => new()
            {
                Id = new TournamentId(Ulid.BogusString(f)),
                Name = f.Random.Words(3),
                Season = f.PickRandom(seasons),
                StartDate = DateOnly.FromDateTime(f.Date.Future()),
                EndDate = DateOnly.FromDateTime(f.Date.Future()),
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
                Winners = [.. f.PickRandom(winners, f.Random.Int(0, 3))]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
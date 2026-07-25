using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Sponsors;

public static class SponsorDetailTournamentResponseFactory
{
    public const string ValidName = "NEBA Championship";
    public static readonly DateOnly ValidStartDate = new(2026, 3, 14);
    public static readonly DateOnly ValidEndDate = new(2026, 3, 15);
    public const bool ValidTitleSponsor = false;

    public static SponsorDetailTournamentResponse Create(
        TournamentId? tournamentId = null,
        string? name = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? titleSponsor = null)
        => new()
        {
            TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
            Name = name ?? ValidName,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            TitleSponsor = titleSponsor ?? ValidTitleSponsor,
        };

    internal static IReadOnlyCollection<SponsorDetailTournamentResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var startDate = faker.Date.PastDateOnly(2);
            return new SponsorDetailTournamentResponse
            {
                TournamentId = Ulid.BogusString(faker),
                Name = faker.Company.CatchPhrase(),
                StartDate = startDate,
                EndDate = startDate.AddDays(faker.Random.Int(0, 2)),
                TitleSponsor = faker.Random.Bool(),
            };
        })];
    }

    public static IReadOnlyCollection<SponsorDetailTournamentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
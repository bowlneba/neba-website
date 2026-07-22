using Neba.Api.Contracts.Tournaments.CreateTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentInputFactory
{
    public const string ValidName = "NEBA Singles";
    public const string ValidTournamentType = "Singles";
    public static readonly DateOnly ValidStartDate = new(2025, 10, 4);
    public static readonly DateOnly ValidEndDate = new(2025, 10, 5);
    public const decimal ValidEntryFee = 100m;

    public static TournamentInput Create(
        string? name = null,
        string? tournamentType = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? statsEligible = null,
        decimal? entryFee = null,
        string? bowlingCenterCertificationNumber = null,
        Uri? externalRegistrationUrl = null,
        TournamentLogoInput? logo = null,
        string? oilPatternId = null,
        string? patternLengthCategory = null,
        string? patternRatioCategory = null)
        => new()
        {
            Name = name ?? ValidName,
            TournamentType = tournamentType ?? ValidTournamentType,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            StatsEligible = statsEligible ?? true,
            EntryFee = entryFee ?? ValidEntryFee,
            BowlingCenterCertificationNumber = bowlingCenterCertificationNumber,
            ExternalRegistrationUrl = externalRegistrationUrl,
            Logo = logo,
            OilPatternId = oilPatternId,
            PatternLengthCategory = patternLengthCategory,
            PatternRatioCategory = patternRatioCategory
        };
}
using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

namespace Neba.TestFactory.Tournaments;

public static class TeamEligibilityMemberResponseFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";
    public const string ValidCountsAs = "NonChampion";

    public static TeamEligibilityMemberResponse Create(
        string? name = ValidName,
        string? usbcId = ValidUsbcId,
        bool? notInSystem = null,
        int? lastTitleYear = null,
        int? titlesLast5Years = null,
        int? titlesLast10Years = null,
        string? countsAs = null,
        bool? ageVerified = null,
        bool? hallOfFameSuperiorPerformance = null,
        bool? titlesBreakRule = null,
        bool? ageBreaksRule = null,
        bool? hallOfFameBreaksRule = null)
        => new()
        {
            Name = name,
            UsbcId = usbcId,
            NotInSystem = notInSystem ?? false,
            LastTitleYear = lastTitleYear,
            TitlesLast5Years = titlesLast5Years ?? 0,
            TitlesLast10Years = titlesLast10Years,
            CountsAs = countsAs ?? ValidCountsAs,
            AgeVerified = ageVerified,
            HallOfFameSuperiorPerformance = hallOfFameSuperiorPerformance,
            TitlesBreakRule = titlesBreakRule ?? false,
            AgeBreaksRule = ageBreaksRule ?? false,
            HallOfFameBreaksRule = hallOfFameBreaksRule ?? false
        };
}

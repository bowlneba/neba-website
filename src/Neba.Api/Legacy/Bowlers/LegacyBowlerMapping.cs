using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Legacy.Bowlers;

// Shared by NewBowlerSyncJob, UpdateBowlerSyncJob and BowlerWeeklySyncJob: how a legacy bowler row's
// coded and free-text columns become website values. Callers log when MapSuffix returns null for a
// non-blank legacy value, since the log message carries each job's own logger type.
internal static class LegacyBowlerMapping
{
    public static Gender? MapGender(int legacyGender)
        => legacyGender switch
        {
            0 => Gender.Male,
            1 => Gender.Female,
            _ => null
        };

    public static DateOnly? MapDateOfBirth(DateTime? legacyDateOfBirth)
        => legacyDateOfBirth.HasValue
            ? DateOnly.FromDateTime(legacyDateOfBirth.Value)
            : null;

    // Legacy Suffix is free text (e.g. "Jr.", "Sr.", "II"); NameSuffix is a closed SmartEnum set
    // whose own Value strings inconsistently carry a trailing period ("Jr.", "Sr." vs. "II", "III").
    // Strip any trailing period from both sides before comparing so "Jr"/"Jr."/"JR." all match
    // NameSuffix.Jr. No match (including a blank/null legacy value) maps to null.
    public static NameSuffix? MapSuffix(string? legacySuffix)
    {
        if (string.IsNullOrWhiteSpace(legacySuffix))
        {
            return null;
        }

        var normalized = legacySuffix.Trim().TrimEnd('.');

        return NameSuffix.List.SingleOrDefault(suffix =>
            string.Equals(suffix.Value.TrimEnd('.'), normalized, StringComparison.OrdinalIgnoreCase));
    }
}

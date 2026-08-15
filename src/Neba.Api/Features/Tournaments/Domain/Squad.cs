using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// A scheduled bowling session within a Tournament. Bowlers (Singles) or teams (Team formats)
/// compete in a Squad to establish a score toward advancement. A Tournament has one or more Squads.
/// </summary>
public sealed class Squad
{
    /// <summary>
    /// Gets the unique identifier for this squad.
    /// </summary>
    public required SquadId Id { get; init; }

    /// <summary>
    /// Gets the date and time (UTC) this squad bowls. Must fall within the owning tournament's
    /// start and end date (inclusive).
    /// </summary>
    public DateTimeOffset BowlingDateTimeUtc { get; private set; }

    /// <summary>
    /// Gets the maximum number of entries (teams for a Team format, bowlers for Singles) that
    /// may bowl this squad, or <see langword="null"/> if uncapped.
    /// </summary>
    public int? MaxEntries { get; private set; }

    /// <summary>
    /// Gets the legacy numeric identifier for this squad, carried over from the previous
    /// system. <see langword="null"/> for squads created after the system migration.
    /// </summary>
    public int? LegacyId { get; internal set; }

    internal Tournament Tournament { get; init; } = null!;

    internal static ErrorOr<Squad> Create(DateTimeOffset bowlingDateTime, int? maxEntries = null, int? legacyId = null)
    {
        var squad = new Squad { Id = SquadId.New(), LegacyId = legacyId };

        var result = squad.UpdateDetails(bowlingDateTime, maxEntries);

        return result.IsError
            ? result.Errors
            : squad;
    }

    internal ErrorOr<Updated> UpdateDetails(DateTimeOffset bowlingDateTime, int? maxEntries)
    {
        if (maxEntries is <= 0)
        {
            return SquadErrors.InvalidMaxEntries(maxEntries.Value);
        }

        BowlingDateTimeUtc = bowlingDateTime.ToUniversalTime();
        MaxEntries = maxEntries;

        return Result.Updated;
    }
}

using Neba.Api.Domain;
using Neba.Api.Features.Stats.Domain;

namespace Neba.Api.Features.Bowlers.Domain;

/// <summary>
/// Represents a bowler in the NEBA system. This is an aggregate root entity that encapsulates all information and behavior related to a bowler.
/// </summary>
/// <remarks>
/// The current implementation is minimal, focused on website display needs. Additional properties for member
/// management (date of birth, gender, membership information, contact details) will be added when migrating
/// from the organization management software.
/// </remarks>
public sealed class Bowler
    : AggregateRoot
{
    /// <summary>
    /// Gets the unique identifier for the bowler.
    /// </summary>
    public required BowlerId Id { get; init; }

    /// <summary>
    /// Gets the bowler's full name (value object containing first name, last name, middle name, suffix, and optional nickname).
    /// </summary>
    public required Name Name { get; init; }

    /// <summary>
    /// Gets the legacy identifier from the existing NEBA website database (used for data migration; maintained for historical reference).
    /// </summary>
    public int? WebsiteId { get; init; }

    /// <summary>
    /// Gets the legacy identifier from the existing organization management software (used for data migration; maintained for historical reference).
    /// </summary>
    public int? LegacyId { get; init; }

    /// <summary>
    /// Gets the bowler's gender (used for agent-specific side cut criteria; will be populated during data migration from the organization management software).
    /// </summary>
    public Gender? Gender { get; init; }

    /// <summary>
    /// Gets the bowler's date of birth (used for age-based side cut criteria; will be populated during data migration from the organization management software).
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    private readonly List<BowlerSeasonStats> _seasonStats = [];
    internal IReadOnlyCollection<BowlerSeasonStats> SeasonStats
        => _seasonStats.AsReadOnly();
}
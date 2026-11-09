
using ErrorOr;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.Api.Features.Stats.Domain;

namespace Neba.Api.Features.Bowlers.Domain;

/// <summary>
/// Represents a bowler in the NEBA system. This is an aggregate root entity that encapsulates all information and behavior related to a bowler.
/// </summary>
/// <remarks>
/// The current implementation is minimal, focused on website display needs. The bowler owns its memberships
/// (see <see cref="AddMembership"/>). Additional properties for member management (contact details) will be
/// added when migrating from the organization management software.
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
    public Name Name { get; internal set; } = null!;

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
    public Gender? Gender { get; internal set; }

    /// <summary>
    /// Gets the bowler's date of birth (used for age-based side cut criteria; will be populated during data migration from the organization management software).
    /// </summary>
    public DateOnly? DateOfBirth { get; internal set; }

    private readonly List<BowlerSeasonStats> _seasonStats = [];
    internal IReadOnlyCollection<BowlerSeasonStats> SeasonStats
        => _seasonStats.AsReadOnly();

    private readonly List<BowlerMembership> _memberships = [];

    /// <summary>
    /// The bowler's memberships, past and current. Must be loaded before calling <see cref="AddMembership"/>:
    /// the eligibility rules read this collection, and an unloaded one would let every purchase pass.
    /// </summary>
    internal IReadOnlyCollection<BowlerMembership> Memberships
        => _memberships.AsReadOnly();

    /// <summary>
    /// Adds a membership of <paramref name="type"/> starting on <paramref name="startDate"/>, or returns
    /// the first eligibility rule the bowler's existing memberships break.
    /// </summary>
    /// <param name="type">The membership type to buy.</param>
    /// <param name="startDate">The first day of the membership, derived from <see cref="TimeProvider"/> by the caller.</param>
    public ErrorOr<Success> AddMembership(MembershipType type, DateOnly startDate)
    {
        var membership = BowlerMembership.Create(
            type,
            startDate,
            [.. _memberships.Select(held => held.ToSnapshot())]);

        if (membership.IsError)
        {
            return membership.Errors;
        }

        _memberships.Add(membership.Value);

        return Result.Success;
    }

    /// <summary>
    /// Adds a membership copied from the organization management software exactly as stored. Runs no eligibility
    /// rules. For the legacy migration and feed only; skip legacy <c>System Import</c> records before calling.
    /// </summary>
    /// <param name="legacyId">The membership's identifier in the legacy database.</param>
    /// <param name="type">The membership type, resolved with <c>MembershipType.FromLegacyId</c>.</param>
    /// <param name="cost">The cost as stored.</param>
    /// <param name="startDate">The start date as stored.</param>
    /// <param name="endDate">The end date as stored.</param>
    internal ErrorOr<Success> AddLegacyMembership(
        int legacyId,
        MembershipType type,
        decimal cost,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (_memberships.Any(held => held.LegacyId == legacyId))
        {
            return BowlerMembershipErrors.LegacyIdAlreadyAdded(legacyId);
        }

        var membership = BowlerMembership.CreateFromLegacy(legacyId, type, cost, startDate, endDate);

        if (membership.IsError)
        {
            return membership.Errors;
        }

        _memberships.Add(membership.Value);

        return Result.Success;
    }

}
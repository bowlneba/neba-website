using System.Diagnostics.CodeAnalysis;

using Neba.Api.Contacts.Domain;
using Neba.Api.Domain;

namespace Neba.Api.Features.BowlingCenters.Domain;

/// <summary>
/// Represents a bowling center.
/// </summary>
public sealed class BowlingCenter
    : AggregateRoot
{
    /// <summary>
    /// The certification number of the bowling center.
    /// </summary>
    public CertificationNumber CertificationNumber { get; init; } = null!;

    /// <summary>
    /// The name of the bowling center.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The status of the bowling center.
    /// </summary>
    public BowlingCenterStatus Status { get; init; } = BowlingCenterStatus.Open;

    /// <summary>
    /// The address of the bowling center.
    /// </summary>
    public Address Address { get; init; } = Address.Empty;

    /// <summary>
    /// The phone numbers of the bowling center.
    /// </summary>
    // Must stay `new List<PhoneNumber>()`, NOT the `[]` collection-expression form: target-typed to
    // the interface property type, `[]` resolves to a fixed-size T[] at runtime, which throws
    // NotSupportedException when EF's owned-collection fixup tries to Add into it while
    // materializing a BowlingCenter with phone numbers. See CLAUDE.md "EF Core Navigation Fixup".
    // Guarded by BowlingCenterTests.PhoneNumbers_DefaultInstance_ShouldSupportAdd_ForEfFixup.
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "See preceding comment — [] would regress to a fixed-size array.")]
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = new List<PhoneNumber>();

    /// <summary>
    /// The email address of the bowling center.
    /// </summary>
    public EmailAddress? EmailAddress { get; init; }

    /// <summary>
    /// The website of the bowling center.
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// The lane configuration of the bowling center.
    /// </summary>
    public LaneConfiguration Lanes { get; init; } = null!;

    /// <summary>
    /// The website ID of the bowling center.
    /// </summary>
    public int? WebsiteId { get; init; }

    /// <summary>
    /// The legacy ID of the bowling center.
    /// </summary>
    public int? LegacyId { get; init; }
}
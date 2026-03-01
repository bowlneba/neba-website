using Neba.Domain.Contact;

namespace Neba.Domain.BowlingCenters;

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
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = [];

    /// <summary>
    /// The email address of the bowling center.
    /// </summary>
    public EmailAddress? EmailAddress { get; init; }

    /// <summary>
    /// The website of the bowling center.
    /// </summary>
    public Uri? Website { get; init; }

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
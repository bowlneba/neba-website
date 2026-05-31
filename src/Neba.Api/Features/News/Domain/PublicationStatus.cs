using Ardalis.SmartEnum;

namespace Neba.Api.Features.News.Domain;

/// <summary>
/// Represents the publication status of a news article. This is implemented as a SmartEnum to allow for potential future expansion (e.g., adding additional statuses such as "Archived" or "Scheduled") without breaking existing code or database schema. The integer value is used for storage and comparison purposes.
/// </summary>
public sealed class PublicationStatus
    : SmartEnum<PublicationStatus>
{
    /// <summary>
    /// Represents a news article that is in draft status (not yet published). Stored as 0 in the database.
    /// </summary>
    public static readonly PublicationStatus Draft = new(nameof(Draft), 0);

    /// <summary>
    /// Represents a news article that has been published and is visible on the website. Stored as 1 in the database.
    /// </summary>
    public static readonly PublicationStatus Published = new(nameof(Published), 1);

    private PublicationStatus(string name, int value)
        : base(name, value)
    { }
}
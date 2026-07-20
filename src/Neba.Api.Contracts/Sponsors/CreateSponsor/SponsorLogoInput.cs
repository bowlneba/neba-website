namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Represents the input required for a sponsor's logo image, already uploaded to storage.
/// </summary>
public sealed record SponsorLogoInput
{
    /// <summary>
    /// The storage container the logo was uploaded to.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The storage path of the uploaded logo.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The MIME content type of the uploaded logo.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The size, in bytes, of the uploaded logo.
    /// </summary>
    public required long SizeInBytes { get; init; }
}

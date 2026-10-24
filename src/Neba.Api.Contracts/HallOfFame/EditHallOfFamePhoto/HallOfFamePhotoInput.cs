namespace Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

/// <summary>
/// An inductee photo already uploaded to storage.
/// </summary>
public sealed record HallOfFamePhotoInput
{
    /// <summary>
    /// The storage container the photo was uploaded to.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The storage path of the uploaded photo.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The MIME content type of the uploaded photo.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The size, in bytes, of the uploaded photo.
    /// </summary>
    public required long SizeInBytes { get; init; }
}
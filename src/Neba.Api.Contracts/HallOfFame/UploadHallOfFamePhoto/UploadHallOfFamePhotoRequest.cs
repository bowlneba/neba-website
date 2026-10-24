using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.HallOfFame.UploadHallOfFamePhoto;

/// <summary>
/// Request model for uploading a Hall of Fame photo.
/// </summary>
public sealed record UploadHallOfFamePhotoRequest
{
    /// <summary>
    /// The image file to be uploaded as an inductee's photo.
    /// </summary>
    public required IFormFile File { get; init; }
}
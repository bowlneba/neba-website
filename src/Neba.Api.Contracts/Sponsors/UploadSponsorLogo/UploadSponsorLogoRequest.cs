using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.Sponsors.UploadSponsorLogo;

/// <summary>
/// Request model for uploading a sponsor logo.
/// </summary>
public sealed record UploadSponsorLogoRequest
{
    /// <summary>
    /// The image file to be uploaded as the sponsor's logo.
    /// </summary>
    public required IFormFile File { get; init; }
}
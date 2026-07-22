using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.Tournaments.UploadTournamentLogo;

/// <summary>
/// Request model for uploading a tournament logo.
/// </summary>
public sealed record UploadTournamentLogoRequest
{
    /// <summary>
    /// The image file to be uploaded as the tournament's logo.
    /// </summary>
    public required IFormFile File { get; init; }
}

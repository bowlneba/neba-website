namespace Neba.Api.Features.Series.UploadSeriesLogo;

/// <summary>Request model for staging a Series logo.</summary>
internal sealed class UploadSeriesLogoRequest
{
    /// <summary>The image file to stage as the Series logo.</summary>
    public required IFormFile File { get; init; }
}
using System.Net.Mime;

using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.UploadSponsorLogo;
using Neba.Api.Uploads;

namespace Neba.Api.Features.Sponsors.UploadSponsorLogo;

internal sealed class UploadSponsorLogoRequestValidator
    : Validator<UploadSponsorLogoRequest>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Webp,
        MediaTypeNames.Image.Gif
    };

    public UploadSponsorLogoRequestValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithErrorCode("UploadSponsorLogoRequest.FileRequired")
            .WithMessage("A file is required.");

        RuleFor(request => request.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode(FileUploadErrors.InvalidContentType.Code)
            .WithMessage("Logo must be JPEG, PNG, WebP, or GIF.")
            .When(request => request.File is not null);

        RuleFor(request => request.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode(FileUploadErrors.FileSizeExceedsLimit.Code)
            .WithMessage($"Logo must not exceed {MaxSizeBytes / (1024 * 1024)} MB.")
            .When(request => request.File is not null);
    }
}
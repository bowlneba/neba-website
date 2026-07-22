using System.Net.Mime;

using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.UploadTournamentLogo;
using Neba.Api.Uploads;

namespace Neba.Api.Features.Tournaments.UploadTournamentLogo;

internal sealed class UploadTournamentLogoRequestValidator
    : Validator<UploadTournamentLogoRequest>
{
    private const long MaxSizeMb = 5;
    private const long MaxSizeBytes = MaxSizeMb * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Webp,
        MediaTypeNames.Image.Gif
    };

    public UploadTournamentLogoRequestValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithErrorCode("UploadTournamentLogoRequest.FileRequired")
            .WithMessage("A file is required.");

        RuleFor(request => request.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode(FileUploadErrors.InvalidContentType.Code)
            .WithMessage("Logo must be JPEG, PNG, WebP, or GIF.")
            .When(request => request.File is not null);

        RuleFor(request => request.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode(FileUploadErrors.FileSizeExceedsLimit.Code)
            .WithMessage($"Logo must not exceed {MaxSizeMb} MB.")
            .When(request => request.File is not null);
    }
}
using System.Net.Mime;

using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.News.UploadArticleHeaderImage;
using Neba.Api.Uploads;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageRequestValidator
    : Validator<UploadArticleHeaderImageRequest>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Webp,
        MediaTypeNames.Image.Gif
    };

    public UploadArticleHeaderImageRequestValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithErrorCode("UploadArticleHeaderImageRequest.FileRequired")
            .WithMessage("A file is required.");

        RuleFor(request => request.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode(FileUploadErrors.InvalidContentType.Code)
            .WithMessage("Header image must be JPEG, PNG, WebP, or GIF.")
            .When(request => request.File is not null);

        RuleFor(request => request.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode(FileUploadErrors.FileSizeExceedsLimit.Code)
            .WithMessage($"Header image must not exceed {MaxSizeBytes / (1024 * 1024)} MB.")
            .When(request => request.File is not null);
    }
}
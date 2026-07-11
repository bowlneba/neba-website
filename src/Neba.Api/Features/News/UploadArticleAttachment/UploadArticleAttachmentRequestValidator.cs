using System.Net.Mime;

using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.News.UploadArticleAttachment;
using Neba.Api.Uploads;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentRequestValidator
    : Validator<UploadArticleAttachmentRequest>
{
    private const long MaxSizeBytes = 25 * 1024 * 1024; // 25 MB

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        MediaTypeNames.Application.Pdf,
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Webp,
        MediaTypeNames.Image.Gif
    };

    public UploadArticleAttachmentRequestValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithErrorCode("UploadArticleAttachment.FileRequired")
            .WithMessage("File is required.");

        RuleFor(request => request.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode(FileUploadErrors.InvalidContentType.Code)
            .WithMessage("Attachment must be a PDF, Word/Excel document, or JPEG/PNG/WebP/GIF image.")
            .When(request => request.File is not null);

        RuleFor(request => request.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode(FileUploadErrors.FileSizeExceedsLimit.Code)
            .WithMessage($"Attachment must not exceed {MaxSizeBytes / (1024 * 1024)} MB.")
            .When(request => request.File is not null);
    }
}
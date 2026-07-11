using ErrorOr;

namespace Neba.Api.Uploads;

internal static class FileUploadValidationRules
{
    public static ErrorOr<Success> HasAllowedContentType(string? contentType, IReadOnlySet<string> allowedContentTypes)
        => contentType is not null && allowedContentTypes.Contains(contentType)
            ? Result.Success
            : Error.Validation(
                code: "FileUpload.InvalidContentType",
                description:"Invalid content type.");

    public static ErrorOr<Success> IsWithinSizeLimit(long lengthInBytes, long maxSizeBytes)
        => lengthInBytes > 0 && lengthInBytes <= maxSizeBytes
            ? Result.Success
            : Error.Validation(
                code: "FileUpload.FileSizeExceedsLimit",
                description: "File size exceeds the limit.",
                metadata: new Dictionary<string, object>
                {
                    { "MaxSizeBytes", maxSizeBytes },
                    { "ActualSizeBytes", lengthInBytes }
                });
}
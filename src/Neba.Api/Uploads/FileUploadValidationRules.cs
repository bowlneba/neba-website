namespace Neba.Api.Uploads;

internal static class FileUploadValidationRules
{
    public static bool HasAllowedContentType(string? contentType, IReadOnlySet<string> allowedContentTypes)
        => contentType is not null && allowedContentTypes.Contains(contentType);

    public static bool IsWithinSizeLimit(long lengthInBytes, long maxSizeBytes)
        => lengthInBytes > 0 && lengthInBytes <= maxSizeBytes;
}
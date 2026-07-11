using ErrorOr;

namespace Neba.Api.Uploads;

internal static class FileUploadErrors
{
    public static Error InvalidContentType
        => Error.Validation(
            code: "FileUpload.InvalidContentType",
            description: "Invalid content type.");

    public static Error FileSizeExceedsLimit
        => Error.Validation(
            code: "FileUpload.FileSizeExceedsLimit",
            description: "File size exceeds the limit.");
}

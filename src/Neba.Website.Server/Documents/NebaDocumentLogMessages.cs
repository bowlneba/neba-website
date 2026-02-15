namespace Neba.Website.Server.Documents;

internal static partial class NebaDocumentLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to initialize NebaDocument TOC for content {ContentId}")]
    public static partial void LogTocInitializationFailed(this ILogger<NebaDocument> logger, Exception exception, string contentId);
}

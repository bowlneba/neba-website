namespace Neba.Api.Features.Documents.RefreshDocument;

internal static partial class RefreshDocumentCommandHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh requested for document {DocumentName}, which isn't listed under Google:Documents in appsettings; nothing was cleared.")]
    public static partial void LogRefreshRequestedForUnlistedDocument(this ILogger<RefreshDocumentCommandHandler> logger, string documentName);
}

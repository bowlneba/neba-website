namespace Neba.Api.Documents;

/// <summary>
/// Where cached document copies live in blob storage.
/// </summary>
internal static class DocumentStorage
{
    internal const string Container = "bowlneba-private";

    internal static string BlobName(string documentName)
        => $"documents/{documentName}";
}
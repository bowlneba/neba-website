using System.Text;

using Audit.Core;

using Azure.Data.Tables;

namespace Neba.Api.Auditing;

/// <summary>
/// Builds the Azure Table entity for an audit event, splitting the event JSON across several
/// string columns. Table Storage rejects any single string property over 32K characters
/// (<c>PropertyValueTooLarge</c>), and a news article update easily exceeds that: the HTML body
/// appears in the request, the response, and the EF column values and changes.
/// </summary>
internal static class ChunkedAuditEventTableEntity
{
    internal const string ColumnName = "AuditEvent";

    // Under the 32,768-character limit, with headroom.
    internal const int ChunkSize = 30_000;

    // Table Storage caps a whole entity at 1 MB (UTF-16, 2 bytes per character).
    // 15 chunks is about 900 KB, which leaves room for the keys and system columns.
    internal const int MaxChunks = 15;

    internal static TableEntity Create(string partitionKey, string rowKey, AuditEvent auditEvent)
    {
        var entity = new TableEntity(partitionKey, rowKey);
        var json = Configuration.JsonAdapter.Serialize(auditEvent);

        var chunkIndex = 0;
        var offset = 0;

        do
        {
            var length = Math.Min(ChunkSize, json.Length - offset);

            // Never split a surrogate pair across two columns.
            if (offset + length < json.Length && char.IsHighSurrogate(json[offset + length - 1]))
            {
                length--;
            }

            entity[ColumnFor(chunkIndex)] = json.Substring(offset, length);
            offset += length;
            chunkIndex++;
        }
        while (offset < json.Length && chunkIndex < MaxChunks);

        if (offset < json.Length)
        {
            // Too big for one entity even when split. Fail the insert (the resilient provider logs
            // it and alerts) rather than store JSON that is cut off and no longer parses.
            throw new InvalidOperationException(
                $"Audit event JSON is {json.Length} characters, which exceeds the {MaxChunks * ChunkSize} characters one table entity can hold.");
        }

        return entity;
    }

    /// <summary>
    /// Joins the columns written by <see cref="Create"/> back into the event JSON.
    /// </summary>
    internal static string ReadJson(TableEntity entity)
    {
        var builder = new StringBuilder();

        for (var chunkIndex = 0; chunkIndex < MaxChunks; chunkIndex++)
        {
            var chunk = entity.GetString(ColumnFor(chunkIndex));

            if (chunk is null)
            {
                break;
            }

            builder.Append(chunk);
        }

        return builder.ToString();
    }

    // The first column keeps the original name so existing rows and readers stay valid.
    private static string ColumnFor(int chunkIndex) =>
        chunkIndex == 0 ? ColumnName : ColumnName + chunkIndex;
}
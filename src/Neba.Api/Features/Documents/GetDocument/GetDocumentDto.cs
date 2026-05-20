namespace Neba.Api.Features.Documents.GetDocument;

internal sealed record GetDocumentDto
{
    public required string Html { get; init; }

    public DateTimeOffset? LastUpdated { get; init; }
}
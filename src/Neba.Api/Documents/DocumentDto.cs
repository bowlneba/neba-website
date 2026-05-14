namespace Neba.Api.Documents;

internal sealed record DocumentDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Content { get; init; }

    public required string ContentType { get; init; }

    public DateTimeOffset? ModifiedAt { get; init; }
}
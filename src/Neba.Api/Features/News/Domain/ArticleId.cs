using StronglyTypedIds;

namespace Neba.Api.Features.News.Domain;

/// <summary>
/// Unique identifier for a news article.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct ArticleId;
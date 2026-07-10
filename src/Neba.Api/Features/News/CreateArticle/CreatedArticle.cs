using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed record CreatedArticle
{
    public required ArticleId Id { get; init; }

    public required string Slug { get; init; }
}
using FastEndpoints;

namespace Neba.Api.Features.News.GetArticle;

internal sealed class GetArticleRequest
{
    [BindFrom("slug")]
    public required string Slug { get; set; }
}

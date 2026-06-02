using FastEndpoints;

namespace Neba.Api.Features.News.ListArticles;

internal sealed class ListArticlesRequest
{
    [BindFrom("page")]
    public int Page { get; set; } = 1;

    [BindFrom("pageSize")]
    public int PageSize { get; set; } = 10;
}
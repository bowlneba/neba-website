using Neba.Api.Contracts.News.EditArticle;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleRequest
{
    public required string Id { get; set; }

    public required EditArticleInput Article { get; set; }
}

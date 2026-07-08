using ErrorOr;

using Neba.Api.Features.News.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed record DeleteArticleCommand
    : ICommand<Deleted>
{
    public required ArticleId ArticleId { get; init; }
}
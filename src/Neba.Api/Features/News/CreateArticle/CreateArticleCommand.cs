using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed record CreateArticleCommand
    : ICommand<CreatedArticle>
{
    public required string Title { get; init; }

    public string? Slug { get; init; }

    public required string Content { get; init; }

    public required PublicationStatus PublicationStatus { get; init; }

    public required DateTimeOffset PublishDateUtc { get; init; }

    public TournamentId? TournamentId { get; init; }
}
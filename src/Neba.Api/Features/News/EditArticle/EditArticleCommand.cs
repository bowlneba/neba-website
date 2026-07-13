using ErrorOr;

using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.EditArticle;

internal sealed record EditArticleCommand
    : ICommand<Updated>
{
    public required ArticleId ArticleId { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required PublicationStatus PublicationStatus { get; init; }

    /// <summary>
    /// The publish date/time, local to the caller (offset embedded). The handler converts this to UTC
    /// before it reaches the domain, which requires UTC.
    /// </summary>
    public required DateTimeOffset PublishDate { get; init; }

    public TournamentId? TournamentId { get; init; }

    public StoredFile? HeaderImage { get; init; }

    public IReadOnlyCollection<EditArticleAttachment> Attachments { get; init; } = [];
}
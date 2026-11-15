namespace Neba.Api.Discord;

/// <summary>
/// A news article to announce: its title, the link readers follow, and the header image when it has one.
/// </summary>
internal sealed record NewsAnnouncement(string Title, Uri ArticleUrl, Uri? ImageUrl);
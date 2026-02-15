using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Documents;
using Neba.Website.Server.Services;

namespace Neba.Website.Server.Documents;

internal sealed class DocumentSlideoverHandler(
    ApiExecutor apiExecutor,
    IDocumentsApi documentsApi)
{
    public MarkupString? Content { get; private set; }
    public string? Title { get; private set; }
    public bool IsLoading { get; private set; }

    public async Task HandleLinkClickedAsync(string pathname, Action stateHasChanged)
    {
        IsLoading = true;
        stateHasChanged();

        var route = pathname.TrimStart('/');
        var documentName = GetDocumentNameFromRoute(route);

        if (documentName is null)
        {
            Content = new MarkupString($"<p>Document not found for route: {pathname}</p>");
            IsLoading = false;
            stateHasChanged();
            return;
        }

        Title = GetDocumentTitle(documentName);

        var result = await apiExecutor.ExecuteAsync(
            "Documents",
            $"Get{documentName}",
            ct => documentsApi.GetDocumentAsync(documentName, ct));

        IsLoading = false;

        if (result.IsError)
        {
            Content = new MarkupString($"<p>Failed to load document: {result.FirstError.Description}</p>");
        }
        else
        {
            Content = new MarkupString(result.Value.Html);
        }

        stateHasChanged();
    }

    internal static string? GetDocumentNameFromRoute(string route)
    {
        return route switch
        {
            "bylaws" => "bylaws",
            "tournaments/rules" => "tournament-rules",
            _ => null
        };
    }

    internal static string GetDocumentTitle(string documentName)
    {
        return documentName switch
        {
            "bylaws" => "Bylaws",
            "tournament-rules" => "Tournament Rules",
            _ => string.Join(' ', documentName.Split('-')
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]))
        };
    }
}

using System.Collections.Concurrent;

using Ganss.Xss;

using HtmlAgilityPack;

using Markdig;

namespace Neba.Website.Server.Help;

/// <summary>
/// Renders the user help documentation embedded from <c>docs/help/*.md</c> (see ADR-0007) for display
/// in <c>HelpButton</c>. Content is checked-in, PR-reviewed
/// markdown, not user input, but the rendered HTML is still passed through <see cref="HtmlSanitizer"/>
/// before ever reaching a <c>MarkupString</c>, matching how the only other raw-HTML-into-<c>MarkupString</c>
/// path in this app (<c>HtmlContentSanitizer</c> in <c>Neba.Api</c>) already behaves.
/// </summary>
internal sealed class HelpDocumentService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private static readonly HtmlSanitizer Sanitizer = new();

    private readonly ConcurrentDictionary<string, string?> _renderedHtmlByDocName = new();

    /// <summary>
    /// Returns the sanitized, rendered HTML for the given kebab-case doc name (e.g. <c>"create-sponsor"</c>
    /// for <c>docs/help/create-sponsor.md</c>), or <c>null</c> if no such doc is embedded. Rendered once
    /// per doc name and cached for the lifetime of the process — the content is static.
    /// </summary>
    public string? GetRenderedHtml(string docName)
        => _renderedHtmlByDocName.GetOrAdd(docName, Render);

    private static string? Render(string docName)
    {
        var markdown = ReadEmbeddedMarkdown(docName);
        if (markdown is null)
        {
            return null;
        }

        var html = Markdown.ToHtml(markdown, Pipeline);
        html = RewriteImageSources(html, docName);
        return Sanitizer.Sanitize(html);
    }

    private static string? ReadEmbeddedMarkdown(string docName)
    {
        var assembly = typeof(HelpDocumentService).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Help.Docs.{docName}.md");
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Rewrites doc-relative image references (e.g. <c>images/create-sponsor/create-form.png</c>, per
    /// ADR-0007's <c>docs/help/images/&lt;feature&gt;/</c> convention) to the <c>/help/images/{doc}/{file}</c>
    /// endpoint that serves the matching embedded resource. Already-absolute sources are left untouched.
    /// </summary>
    private static string RewriteImageSources(string html, string docName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var images = doc.DocumentNode.SelectNodes("//img[@src]");
        if (images is null)
        {
            return html;
        }

        foreach (var image in images)
        {
            var src = image.GetAttributeValue("src", string.Empty);
            if (src.Length == 0 || src.StartsWith('/') || Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
                continue;
            }

            var fileName = Path.GetFileName(src);
            if (fileName.Length > 0)
            {
                image.SetAttributeValue("src", $"/help/images/{docName}/{fileName}");
            }
        }

        return doc.DocumentNode.OuterHtml;
    }
}
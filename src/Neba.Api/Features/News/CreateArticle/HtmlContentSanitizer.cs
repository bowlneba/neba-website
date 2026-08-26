using System.Net;
using System.Text.RegularExpressions;

using Ganss.Xss;

using HtmlAgilityPack;

namespace Neba.Api.Features.News.CreateArticle;

/// <summary>
/// Sanitizes rich-text HTML (e.g. Quill editor output) before it is persisted, stripping any
/// construct that could lead to XSS when later rendered via <c>MarkupString</c> — script tags,
/// event handler attributes, non-http(s)/mailto URL schemes (including <c>data:</c> URIs), and
/// non-allowlisted tags such as &lt;iframe&gt;/&lt;object&gt;/&lt;embed&gt;. Feature-agnostic by
/// design (sibling to <c>Compliance</c>, not nested under any one feature's domain folder) so any
/// future rich-text field can reuse it, not just <c>Article.Content</c>.
/// </summary>
/// <remarks>
/// Also linkifies bare URLs (e.g. "BowlNEBA.com/Tournaments" typed as plain text rather than
/// inserted via the editor's "link" toolbar button) into real &lt;a&gt; elements, since Quill has
/// no auto-link module — plain-text URLs are otherwise persisted verbatim and never render as
/// clickable links.
/// </remarks>
internal static partial class HtmlContentSanitizer
{
    // Stryker disable once all : HtmlSanitizer is a third-party allowlist sanitizer; its default
    // configuration is the tested surface, not a mutable rule set authored here. mailto is added
    // because the default AllowedSchemes is http(s)-only and would otherwise silently strip
    // mailto: links from article content.
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    // Stryker disable once all : trivial factory around third-party defaults, see above.
    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedSchemes.Add("mailto");
        return sanitizer;
    }

    internal static string Sanitize(string html)
        => Sanitizer.Sanitize(Linkify(html));

    private static string Linkify(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var textNodes = doc.DocumentNode
            .SelectNodes("//text()[not(ancestor::a) and not(ancestor::script) and not(ancestor::style)]");

        if (textNodes is null)
        {
            return html;
        }

        foreach (var textNode in textNodes.ToList())
        {
            LinkifyTextNode(textNode);
        }

        return doc.DocumentNode.InnerHtml;
    }

    private static void LinkifyTextNode(HtmlNode textNode)
    {
        var text = textNode.InnerText;
        var matches = BareUrlRegex().Matches(text);

        if (matches.Count == 0)
        {
            return;
        }

        var replacementHtml = new System.Text.StringBuilder();
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            replacementHtml.Append(WebUtility.HtmlEncode(text[lastIndex..match.Index]));

            var displayText = match.Value;
            var href = displayText.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || displayText.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? displayText
                : "https://" + displayText;

            replacementHtml
                .Append("<a href=\"")
                .Append(WebUtility.HtmlEncode(href))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(displayText))
                .Append("</a>");

            lastIndex = match.Index + match.Length;
        }

        replacementHtml.Append(WebUtility.HtmlEncode(text[lastIndex..]));

        var wrapper = new HtmlDocument();
        wrapper.LoadHtml("<span>" + replacementHtml + "</span>");
        var wrapperNode = wrapper.DocumentNode.SelectSingleNode("//span");

        var referenceNode = textNode;
        foreach (var child in wrapperNode?.ChildNodes.ToList() ?? [])
        {
            textNode.ParentNode?.InsertAfter(child, referenceNode);
            referenceNode = child;
        }

        textNode.ParentNode?.RemoveChild(textNode);
    }

    /// <summary>
    /// Matches bare URLs typed as plain text: full http(s) URLs, www.-prefixed hosts, and bare
    /// domains with a common TLD (with or without a trailing path) — e.g. "BowlNEBA.com/Stats".
    /// Trailing sentence punctuation (periods, commas, parens, etc.) is deliberately excluded from
    /// the match so links don't swallow the punctuation that follows them in prose.
    /// </summary>
    [GeneratedRegex(
        @"\b(?:https?://[^\s<]+[^\s<.,;:!?)\]}]|www\.[^\s<]+[^\s<.,;:!?)\]}]|[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.(?:com|org|net|edu|gov|io|co|us|info|biz)(?:/[^\s<]*[^\s<.,;:!?)\]}])?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex BareUrlRegex();
}
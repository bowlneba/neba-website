using Ganss.Xss;

namespace Neba.Api.Features.News.CreateArticle;

/// <summary>
/// Sanitizes rich-text HTML (e.g. Quill editor output) before it is persisted, stripping any
/// construct that could lead to XSS when later rendered via <c>MarkupString</c> — script tags,
/// event handler attributes, non-http(s)/mailto URL schemes (including <c>data:</c> URIs), and
/// non-allowlisted tags such as &lt;iframe&gt;/&lt;object&gt;/&lt;embed&gt;. Feature-agnostic by
/// design (sibling to <c>Compliance</c>, not nested under any one feature's domain folder) so any
/// future rich-text field can reuse it, not just <c>Article.Content</c>.
/// </summary>
internal static class HtmlContentSanitizer
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
        => Sanitizer.Sanitize(html);
}
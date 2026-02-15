using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace Neba.Infrastructure.Documents;

internal sealed partial class HtmlProcessor(GoogleSettings googleDriveSettings)
{
    private readonly GoogleSettings _settings = googleDriveSettings;

    public string Process(string rawHtml)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);

        // Extract body content only
        var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
        if (bodyNode is null)
        {
            return rawHtml; // Return original if no body found
        }

        // Generate anchor Ids from headings
        GenerateAnchorIds(bodyNode);

        // Replace Google Docs links with internal routes
        ReplaceGoogleDocsLinks(bodyNode);

        // Extract and preserve Google Docs list styles
        var listStyles = ExtractGoogleDocsListStyles(doc.DocumentNode);

        // Return body content with list styles prepended
        return string.IsNullOrEmpty(listStyles)
            ? bodyNode.InnerHtml
            : $"<style>{listStyles}</style>{bodyNode.InnerHtml}";
    }

    /// <summary>
    /// Generates human-readable anchor IDs for all heading elements.
    /// </summary>
    /// <remarks>
    /// Transforms heading text into anchor IDs:
    /// "Section 10.3 - Hall of Fame" → "section-10.3-hall-of-fame"
    /// </remarks>
    private static void GenerateAnchorIds(HtmlNode node)
    {
        var headings = node.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
        if (headings is null)
        {
            return; // No headings found
        }

        foreach (var heading in headings)
        {
            // Preserve original ID (e.g., Google Docs "h.xk7tre4v41xy") so anchor
            // links using the original fragment can be resolved by the JS anchor lookup.
            var originalId = heading.GetAttributeValue("id", string.Empty);
            if (!string.IsNullOrEmpty(originalId))
            {
                heading.SetAttributeValue("data-original-id", originalId);
            }

            var anchorId = GenerateAnchorId(heading.InnerText);
            heading.SetAttributeValue("id", anchorId);
        }
    }

    /// <summary>
    /// Replaces Google Docs URLs with internal application routes.
    /// </summary>
    /// <remarks>
    /// Handles both direct and redirect URLs:
    /// - Direct: https://docs.google.com/document/d/{documentId}/...
    /// - Redirect: https://www.google.com/url?q=https://docs.google.com/document/...
    /// Replacement: Configured route from settings (e.g., "/about/bylaws")
    /// Also transforms anchor-only links (#h.xk7tre4v41xy) to use human-readable IDs
    /// </remarks>
    private void ReplaceGoogleDocsLinks(HtmlNode node)
    {
        // Build anchor lookup map: Google Docs ID → human-readable ID
        var anchorLookup = BuildAnchorLookup(node);

        var links = node.SelectNodes("//a[@href]");
        if (links is null)
        {
            return; // No links found
        }

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(href))
            {
                continue; // Skip links without href
            }

            // Handle anchor-only links within the same document (e.g., "#h.xk7tre4v41xy")
            if (href.StartsWith('#'))
            {
                var anchorId = href[1..]; // Remove leading #

                // Strip "heading=" prefix if present
                if (anchorId.StartsWith("heading=", StringComparison.OrdinalIgnoreCase))
                {
                    anchorId = anchorId[8..]; // Skip "heading="
                }

                // Look up human-readable ID
                if (anchorLookup.TryGetValue(anchorId, out var humanReadableId))
                {
                    link.SetAttributeValue("href", $"#{humanReadableId}");
                }

                continue;
            }

            // Check if this is a Google redirect URL (e.g., https://www.google.com/url?q=...)
            // If so, extract the actual URL from the 'q' parameter
            var urlToMatch = href;
            var match = GoogleRedirectUrlRegex().Match(href);
            if (match.Success)
            {
                // Extract URL from 'q' parameter and decode it
                var encodedUrl = match.Groups["url"].Value;
                urlToMatch = Uri.UnescapeDataString(encodedUrl);
            }

            // Match Google Docs URL pattern
            match = GoogleDocsUrlRegex().Match(urlToMatch);
            if (!match.Success)
            {
                continue; // Not a Google Docs link
            }

            var documentId = match.Groups["documentId"].Value;

            // Find matching document in configuration
            var document = _settings.Documents
                .FirstOrDefault(d => d.DocumentId == documentId);

            if (document is null)
            {
                continue; // No matching document found in settings
            }

            // Extract anchor if present
            var anchorIndex = urlToMatch.IndexOf('#', StringComparison.OrdinalIgnoreCase);
            var anchor = string.Empty;

            if (anchorIndex >= 0)
            {
                anchor = urlToMatch[anchorIndex..];

                // Strip Google Docs "heading=" prefix if present
                // Convert "#heading=h.abc123" → "#h.abc123"
                if (anchor.StartsWith("#heading=", StringComparison.OrdinalIgnoreCase))
                {
                    anchor = "#" + anchor[9..]; // Skip "#heading="
                }
            }

            // Replace with internal route
            var webRoute = document.WebRoute.StartsWith('/')
                ? document.WebRoute
                : "/" + document.WebRoute;
            link.SetAttributeValue("href", webRoute + anchor);
        }
    }

    /// <summary>
    /// Builds a lookup map from Google Docs heading IDs to human-readable IDs.
    /// </summary>
    /// <param name="node">The HTML node to search for headings.</param>
    /// <returns>Dictionary mapping original Google Docs IDs to human-readable IDs.</returns>
    private static Dictionary<string, string> BuildAnchorLookup(HtmlNode node)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headings = node.SelectNodes("//h1[@data-original-id] | //h2[@data-original-id] | //h3[@data-original-id] | //h4[@data-original-id] | //h5[@data-original-id] | //h6[@data-original-id]");

        if (headings is null)
        {
            return lookup;
        }

        foreach (var heading in headings)
        {
            var originalId = heading.GetAttributeValue("data-original-id", string.Empty);
            var humanReadableId = heading.GetAttributeValue("id", string.Empty);

            if (!string.IsNullOrEmpty(originalId) && !string.IsNullOrEmpty(humanReadableId))
            {
                lookup[originalId] = humanReadableId;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Extracts Google Docs list-specific CSS rules from the document head.
    /// </summary>
    /// <param name="documentNode">The root HTML document node.</param>
    /// <returns>Filtered CSS rules for list styling, or empty string if none found.</returns>
    /// <remarks>
    /// Google Docs exports list formatting (e.g., lower-alpha for a, b, c) in &lt;style&gt; tags
    /// using CSS classes like .lst-kix_* with list-style-type rules. This method extracts and
    /// filters only the list-related CSS to preserve the original formatting.
    /// </remarks>
    private static string ExtractGoogleDocsListStyles(HtmlNode documentNode)
    {
        // Find all style tags in the head
        var styleNodes = documentNode.SelectNodes("//head/style");
        if (styleNodes is null || styleNodes.Count == 0)
        {
            return string.Empty;
        }

        var listStyleRules = styleNodes
            .Select(styleNode => styleNode.InnerText)
            .Where(cssContent => !string.IsNullOrWhiteSpace(cssContent))
            .SelectMany(cssContent => GoogleDocsListStyleRegex().Matches(cssContent).Cast<Match>())
            .Select(match => match.Value)
            .ToList();

        return listStyleRules.Count > 0
            ? string.Join(string.Empty, listStyleRules)
            : string.Empty;
    }

    /// <summary>
    /// Generates a URL-safe anchor ID from heading text.
    /// </summary>
    /// <param name="text">Heading text to convert.</param>
    /// <returns>URL-safe anchor ID.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Globalization",
    "CA1308:Normalize strings to uppercase",
    Justification = "Anchor IDs follow web conventions using lowercase kebab-case for URLs")]
    private static string GenerateAnchorId(string text)
    {
        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Replace spaces and punctuation with hyphens
        text = AnchorIdRegex().Replace(text, "-");

        // Remove consecutive hyphens
        text = ConsecutiveHyphensRegex().Replace(text, "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        return text;
    }

    /// <summary>
    /// Regex pattern for matching Google redirect URLs with embedded document URLs.
    /// </summary>
    /// <remarks>
    /// Matches URLs like: https://www.google.com/url?q=https://docs.google.com/document/...
    /// Captures the URL in the 'q' query parameter.
    /// </remarks>
    [GeneratedRegex(@"https://www\.google\.com/url\?q=(?<url>[^&]+)")]
    private static partial Regex GoogleRedirectUrlRegex();

    /// <summary>
    /// Regex pattern for matching Google Docs URLs.
    /// </summary>
    /// <remarks>
    /// Matches both patterns:
    /// - https://docs.google.com/document/d/{documentId}/...
    /// - https://docs.google.com/document/u/{userId}/d/{documentId}/...
    /// </remarks>
    [GeneratedRegex(@"https://docs\.google\.com/document/(?:u/\d+/)?d/(?<documentId>[^/]+)")]
    private static partial Regex GoogleDocsUrlRegex();

    /// <summary>
    /// Regex pattern for replacing non-alphanumeric characters (except periods and hyphens).
    /// </summary>
    [GeneratedRegex(@"[^a-z0-9.\-]+")]
    private static partial Regex AnchorIdRegex();

    /// <summary>
    /// Regex pattern for replacing consecutive hyphens.
    /// </summary>
    [GeneratedRegex(@"-{2,}")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Regex pattern for matching Google Docs list CSS rules (.lst-kix_* classes).
    /// </summary>
    /// <remarks>
    /// Matches CSS rules like: .lst-kix_abc123-0{list-style-type:lower-alpha}
    /// Captures the complete rule including selector, braces, and properties.
    /// </remarks>
    [GeneratedRegex(@"\.lst-kix_[^{]+\{[^}]*list-style-type:[^}]+\}")]
    private static partial Regex GoogleDocsListStyleRegex();
}
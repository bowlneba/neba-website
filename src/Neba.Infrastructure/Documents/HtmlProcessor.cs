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

        return bodyNode.InnerHtml;
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
    /// Pattern: https://docs.google.com/document/d/{documentId}/...
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

            // Match Google Docs URL pattern
            var match = GoogleDocsUrlRegex().Match(href);
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
            var anchorIndex = href.IndexOf('#', StringComparison.OrdinalIgnoreCase);
            var anchor = string.Empty;

            if (anchorIndex >= 0)
            {
                anchor = href[anchorIndex..];

                // Strip Google Docs "heading=" prefix if present
                // Convert "#heading=h.abc123" → "#h.abc123"
                if (anchor.StartsWith("#heading=", StringComparison.OrdinalIgnoreCase))
                {
                    anchor = "#" + anchor[9..]; // Skip "#heading="
                }
            }

            // Replace with internal route
            link.SetAttributeValue("href", document.WebRoute + anchor);
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
    /// Regex pattern for matching Google Docs URLs.
    /// </summary>
    [GeneratedRegex(@"https://docs\.google\.com/document/d/(?<documentId>[^/]+)")]
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
}
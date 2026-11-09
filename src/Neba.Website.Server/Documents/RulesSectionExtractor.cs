using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace Neba.Website.Server.Documents;

/// <summary>Cuts one rule section out of the full rules HTML.</summary>
internal static partial class RulesSectionExtractor
{
    [GeneratedRegex(@"^rule-\d+-", RegexOptions.CultureInvariant)]
    private static partial Regex RuleHeadingId();

    /// <summary>
    /// Returns the heading that <paramref name="alias"/> points to, and its content up to the next heading
    /// of the same or higher level. Same-document links (<c>#...</c>) point at <paramref name="rulesPath"/>.
    /// Returns <c>null</c> when no heading matches.
    /// </summary>
    internal static string? Extract(string html, AnchorAlias alias, string rulesPath)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var start = ResolveHeading(doc, alias);
        if (start is null)
        {
            return null;
        }

        var level = HeadingLevel(start)!.Value;
        var section = HtmlNode.CreateNode("<div></div>");

        // Google Docs list numbering lives in <style> blocks, so carry them over.
        foreach (var style in doc.DocumentNode.Descendants("style"))
        {
            section.AppendChild(style.CloneNode(true));
        }

        for (var node = start; node is not null; node = NextElement(node))
        {
            var nodeLevel = HeadingLevel(node);
            if (node != start && nodeLevel is { } l && l <= level)
            {
                break;
            }

            section.AppendChild(node.CloneNode(true));
        }

        foreach (var link in section.Descendants("a").Where(a => a.GetAttributeValue("href", string.Empty).StartsWith('#')))
        {
            link.SetAttributeValue("href", rulesPath + link.GetAttributeValue("href", string.Empty));
        }

        return section.InnerHtml;
    }

    // Number first (heading ID starts with the prefix and its text has the keyword); if the rules were
    // renumbered, the first rule heading whose text has the keyword. Lettered sub-rules never match
    // because only IDs shaped like "rule-<digits>-" count.
    private static HtmlNode? ResolveHeading(HtmlDocument doc, AnchorAlias alias)
    {
        var ruleHeadings = doc.DocumentNode
            .Descendants()
            .Where(n => HeadingLevel(n) is not null && RuleHeadingId().IsMatch(n.GetAttributeValue("id", string.Empty)))
            .ToList();

        bool HasKeyword(HtmlNode n)
            => HtmlEntity.DeEntitize(n.InnerText).Contains(alias.TitleKeyword, StringComparison.OrdinalIgnoreCase);

        var byNumber = ruleHeadings.Find(n => n.GetAttributeValue("id", string.Empty)
            .StartsWith(alias.HeadingIdPrefix, StringComparison.Ordinal));

        return byNumber is not null && HasKeyword(byNumber)
            ? byNumber
            : ruleHeadings.Find(HasKeyword);
    }

    private static int? HeadingLevel(HtmlNode node)
        => node.Name.Length == 2 && node.Name[0] is 'h' && node.Name[1] is >= '1' and <= '6'
            ? node.Name[1] - '0'
            : null;

    private static HtmlNode? NextElement(HtmlNode node)
    {
        var next = node.NextSibling;
        while (next is not null && next.NodeType != HtmlNodeType.Element)
        {
            next = next.NextSibling;
        }

        return next;
    }
}

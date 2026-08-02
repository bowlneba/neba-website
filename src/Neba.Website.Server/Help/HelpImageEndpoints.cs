namespace Neba.Website.Server.Help;

/// <summary>
/// Serves the screenshots embedded from <c>docs/help/images/&lt;doc&gt;/</c> (see ADR-0007) that
/// <see cref="HelpDocumentService"/> rewrites help doc <c>&lt;img&gt;</c> sources to point at.
/// </summary>
internal static class HelpImageEndpoints
{
    extension(WebApplication app)
    {
        public void MapHelpImageEndpoints()
            => app.MapGet("/help/images/{doc}/{file}", GetImage).RequireAuthorization();
    }

    internal static IResult GetImage(string doc, string file)
    {
        var assembly = typeof(HelpImageEndpoints).Assembly;
        var stream = assembly.GetManifestResourceStream($"Help.Images.{doc}/{file}");

        return stream is null
            ? Results.NotFound()
            : Results.File(stream, GetContentType(file));
    }

    internal static string GetContentType(string fileName) => Path.GetExtension(fileName) switch
    {
        var ext when ext.Equals(".png", StringComparison.OrdinalIgnoreCase) => "image/png",
        var ext when ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) => "image/jpeg",
        var ext when ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) => "image/gif",
        var ext when ext.Equals(".svg", StringComparison.OrdinalIgnoreCase) => "image/svg+xml",
        _ => "application/octet-stream",
    };
}
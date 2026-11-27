namespace Neba.Api.Tests.Encryption;

internal static class RepoFiles
{
    // Copied beside the tests by Neba.Api.Tests.csproj, so tests read the committed files.
    public static string DevelopmentSettings
        => Path.Combine(AppContext.BaseDirectory, "Encryption", "Samples", "api.appsettings.Development.json");

    public static string BrowserSample
        => Path.Combine(AppContext.BaseDirectory, "Encryption", "Samples", "browser-sample.json");
}
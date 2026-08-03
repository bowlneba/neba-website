namespace Neba.Api.Legacy;

/// <summary>
/// Configuration for the temporary `/legacy` backdoor (see docs/api/software-backdoor-plan.md).
/// Deleted along with the rest of Legacy/ at Software sunset.
/// </summary>
internal sealed record LegacySettings
{
    /// <summary>
    /// Shared secret the Software presents via the <c>X-Api-Key</c> header on every `/legacy` request.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Plain ADO.NET connection string to the Software's own database (`neba-fwk`, Azure SQL).
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
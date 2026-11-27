namespace Neba.Api.Legacy;

/// <summary>
/// Configuration for the temporary `/legacy` backdoor (see docs/api/software-backdoor-plan.md).
/// Deleted along with the rest of Legacy/ at Software sunset.
/// </summary>
// A class, not a record: a record would print the secrets from ToString().
internal sealed class LegacySettings
{
    /// <summary>
    /// Shared secret the Software presents via the <c>X-Api-Key</c> header on every `/legacy` request.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Plain ADO.NET connection string to the Software's own database (`neba-fwk`, Azure SQL).
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Passphrase the Software encrypted SSNs with. Required outside Development.
    /// </summary>
    public string SsnPassphrase { get; init; } = string.Empty;
}
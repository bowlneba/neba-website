namespace Neba.Website.Server.Maps;

/// <summary>
/// Configuration settings for Azure Maps integration.
/// </summary>
public sealed class AzureMapsSettings
{
    internal const string SectionName = "AzureMaps";

    /// <summary>
    /// The Azure Maps account's unique ID, sent as the <c>x-ms-client-id</c> header on direct
    /// REST calls (route calculation, search). Injected via app settings in production
    /// (provisioned by <c>maps.bicep</c>); set manually in user secrets for local development.
    /// </summary>
    /// <remarks>
    /// Production currently authenticates with <see cref="SubscriptionKey"/>, not managed
    /// identity/AAD — see issue #28. This field is populated regardless, since the JS layer
    /// also sends it as <c>x-ms-client-id</c> alongside the subscription key.
    /// </remarks>
    public string? AccountId { get; set; }

    /// <summary>
    /// The subscription key for Azure Maps. Stored in Key Vault in production
    /// (written directly by <c>maps.bicep</c> at provisioning time) and in user secrets locally.
    /// </summary>
    public string? SubscriptionKey { get; set; }
}
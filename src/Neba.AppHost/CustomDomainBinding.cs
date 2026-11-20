using Aspire.Hosting.Azure;

using Azure.Provisioning.AppContainers;

namespace Neba.AppHost;

/// <summary>
/// A custom hostname and the parameter that holds the name of its managed certificate.
/// </summary>
internal sealed record CustomDomainBinding(
    IResourceBuilder<ParameterResource> Domain,
    IResourceBuilder<ParameterResource> CertificateName)
{
    /// <summary>
    /// Declares the parameters and returns the binding, or <see langword="null"/> when the domain
    /// parameter has no value.
    /// </summary>
    /// <remarks>
    /// This reads configuration (<c>Parameters__&lt;name&gt;</c> or user secrets) only, not a value
    /// that Aspire prompts for or keeps in deploy state. An environment with no DNS (staging)
    /// therefore deploys without binding any hostname.
    /// </remarks>
    internal static CustomDomainBinding? AddIfConfigured(
        IDistributedApplicationBuilder builder,
        string domainParameterName,
        string certificateParameterName)
        => string.IsNullOrWhiteSpace(builder.Configuration[$"Parameters:{domainParameterName}"])
            ? null
            : new CustomDomainBinding(
                builder.AddParameter(domainParameterName),
                builder.AddParameter(certificateParameterName).AsOptional());

    /// <summary>
    /// Binds the hostname and its certificate to the container app.
    /// </summary>
#pragma warning disable ASPIREACADOMAINS001
    internal void ApplyTo(ContainerApp app)
        => app.ConfigureCustomDomain(Domain, CertificateName);
#pragma warning restore ASPIREACADOMAINS001
}

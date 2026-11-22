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
    /// The setting (<c>CustomDomains__AllowNone</c>) that lets a deploy run with no domain. Only staging
    /// sets it, because it has no DNS.
    /// </summary>
    internal const string AllowNoneSetting = "CustomDomains:AllowNone";

    /// <summary>
    /// Declares the parameters and returns the binding. A blank domain parameter fails the deploy,
    /// unless the environment sets <see cref="AllowNoneSetting"/>, in which case it returns
    /// <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// This reads configuration (<c>Parameters__&lt;name&gt;</c> or user secrets) only, not a value
    /// that Aspire prompts for or keeps in deploy state. A renamed or empty repo variable must not
    /// deploy the app without its production hostname.
    /// </remarks>
    internal static CustomDomainBinding? AddRequired(
        IDistributedApplicationBuilder builder,
        string domainParameterName,
        string certificateParameterName)
    {
        var binding = AddIfConfigured(builder, domainParameterName, certificateParameterName);

        return binding is null && !string.Equals(builder.Configuration[AllowNoneSetting], "true", StringComparison.OrdinalIgnoreCase)
            ? throw new InvalidOperationException(
                $"Parameters:{domainParameterName} has no value. Set it, or set {AllowNoneSetting}=true for an environment with no DNS.")
            : binding;
    }

    /// <summary>
    /// Declares the parameters and returns the binding, or <see langword="null"/> when the domain
    /// parameter has no value.
    /// </summary>
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
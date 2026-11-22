using Aspire.Hosting;

using Neba.AppHost;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.AppHost;

[UnitTest]
[Component("AppHost.CustomDomainBinding")]
public sealed class CustomDomainBindingTests
{
    private const string DomainName = "apiCustomDomain";
    private const string CertificateName = "apiCertificateName";

    [Fact(DisplayName = "AddIfConfigured should return null and declare no parameters when the domain has no value")]
    public void AddIfConfigured_ShouldReturnNull_WhenDomainIsNotConfigured()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var binding = CustomDomainBinding.AddIfConfigured(builder, DomainName, CertificateName);

        // Assert
        binding.ShouldBeNull();
        builder.Resources.ShouldNotContain(r => r.Name == DomainName);
        builder.Resources.ShouldNotContain(r => r.Name == CertificateName);
    }

    [Fact(DisplayName = "AddIfConfigured should return null when the domain is blank")]
    public void AddIfConfigured_ShouldReturnNull_WhenDomainIsBlank()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([$"--Parameters:{DomainName}= "]);

        // Act
        var binding = CustomDomainBinding.AddIfConfigured(builder, DomainName, CertificateName);

        // Assert
        binding.ShouldBeNull();
    }

    [Fact(DisplayName = "AddIfConfigured should return a binding with both parameters when the domain has a value")]
    public void AddIfConfigured_ShouldReturnBinding_WhenDomainIsConfigured()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([$"--Parameters:{DomainName}=api.example.com"]);

        // Act
        var binding = CustomDomainBinding.AddIfConfigured(builder, DomainName, CertificateName);

        // Assert
        binding.ShouldNotBeNull();
        binding.Domain.Resource.Name.ShouldBe(DomainName);
        binding.CertificateName.Resource.Name.ShouldBe(CertificateName);
        builder.Resources.ShouldContain(r => r.Name == DomainName);
        builder.Resources.ShouldContain(r => r.Name == CertificateName);
    }
}

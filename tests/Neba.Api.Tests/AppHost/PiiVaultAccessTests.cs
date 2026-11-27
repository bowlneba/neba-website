using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Testing;

using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.AppHost;

/// <summary>
/// Only the api may reach the vault that holds the keys for sensitive data (ADR-0011).
/// </summary>
[IntegrationTest]
[Component("AppHost.PiiVaultAccess")]
public sealed class PiiVaultAccessTests
{
    private const string PiiVault = "piivault";

    // Builds the model a deploy would, without starting or provisioning anything.
    private static Task<IDistributedApplicationTestingBuilder> BuildPublishModelAsync()
        => DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(
            ["--operation", "publish", "--publisher", "manifest", "--CustomDomains:AllowNone=true"],
            TestContext.Current.CancellationToken);

    private static bool ReferencesPiiVault(IResource resource)
        => resource.Annotations.OfType<ResourceRelationshipAnnotation>().Any(a => a.Resource.Name == PiiVault);

    private static bool HasPiiVaultRole(IResource resource)
        => resource.Annotations.OfType<RoleAssignmentAnnotation>().Any(a => a.Target.Name == PiiVault);

    [Fact(DisplayName = "Only the api should reference the PII vault or hold a role on it")]
    public async Task AppModel_ShouldGiveOnlyTheApiAccess_WhenPublishing()
    {
        // Arrange
        await using var builder = await BuildPublishModelAsync();
        var apps = builder.Resources.Where(r => r is ProjectResource || r.Name is "api" or "web").ToList();

        // Act
        var referencing = apps.Where(ReferencesPiiVault).Select(r => r.Name).ToList();
        var holdingRoles = apps.Where(HasPiiVaultRole).Select(r => r.Name).ToList();

        // Assert
        apps.Select(r => r.Name).ShouldContain("web");
        referencing.ShouldBe(["api"]);
        holdingRoles.ShouldBe(["api"]);
    }
}
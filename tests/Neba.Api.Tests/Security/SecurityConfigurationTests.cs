using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[IntegrationTest]
[Component("Security")]
public sealed class SecurityConfigurationTests
{
    [Fact(DisplayName = "UseSecurityInfrastructure should return the same WebApplication instance")]
    public void UseSecurityInfrastructure_ShouldReturnSameApp()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        var app = builder.Build();

        // Act
        var result = app.UseSecurityInfrastructure();

        // Assert
        result.ShouldBeSameAs(app);
    }
}
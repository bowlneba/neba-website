using Hangfire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

/// <summary>
/// Calls the real <c>AddBackgroundJobs</c> registration. Hangfire's storage, filters and server are all
/// registered as deferred factories, so nothing static is touched until a provider resolves them,
/// and these tests never resolve them.
/// </summary>
[UnitTest]
[Component("BackgroundJobs")]
public sealed class DomainEventPublisherRegistrationTests
{
    [Fact(DisplayName = "AddBackgroundJobs should register IDomainEventPublisher once, as a singleton DomainEventPublisher")]
    public void AddBackgroundJobs_ShouldRegisterPublisherAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddBackgroundJobs(configuration);

        // Assert
        var descriptor = services.Where(d => d.ServiceType == typeof(IDomainEventPublisher)).ShouldHaveSingleItem();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(DomainEventPublisher));
    }

    [Fact(DisplayName = "The container built from AddBackgroundJobs should resolve the same IDomainEventPublisher for every scope")]
    public void AddBackgroundJobs_ShouldResolveSamePublisherAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBackgroundJobs(new ConfigurationBuilder().Build());
        // Replaces Hangfire's own client registration (the last one wins); the publisher is the only service resolved.
        services.AddSingleton(new Mock<IBackgroundJobClient>(MockBehavior.Strict).Object);
        using var provider = services.BuildServiceProvider();

        // Act
        var fromRoot = provider.GetRequiredService<IDomainEventPublisher>();
        using var scope = provider.CreateScope();
        var fromScope = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        // Assert
        fromRoot.ShouldBeOfType<DomainEventPublisher>();
        fromScope.ShouldBeSameAs(fromRoot);
    }
}
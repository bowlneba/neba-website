using System.Text.RegularExpressions;

using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;

using Neba.Api.Database;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Architecture;

[UnitTest]
[Component("Architecture")]
public sealed partial class FeatureBoundaryTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(AppDbContext).Assembly).Build();

    [GeneratedRegex(@"^Neba\.Api\.Features\.(?<feature>[^.]+)\.Domain(\.|$)")]
    private static partial Regex FeatureDomainNamespace();

    [GeneratedRegex(@"^Neba\.Api\.Features\.(?<feature>[^.]+)(\.(?!Domain(\.|$))|$)")]
    private static partial Regex FeatureUseCaseNamespace();

    private static string? FeatureOfUseCaseType(IType type)
    {
        var match = FeatureUseCaseNamespace().Match(type.Namespace.FullName);
        return match.Success ? match.Groups["feature"].Value : null;
    }

    private static string? FeatureOfDomainType(IType type)
    {
        var match = FeatureDomainNamespace().Match(type.Namespace.FullName);
        return match.Success ? match.Groups["feature"].Value : null;
    }

    // Existing violations. The list may only shrink.
    private static readonly string[] KnownSliceViolations =
    [
    ];

    [Fact(DisplayName = "Use-case code should not depend on another feature's use-case code")]
    public void UseCases_ShouldNotDependOnOtherFeaturesUseCases()
    {
        // Arrange
        var useCaseTypes = Architecture.Types
            .Where(t => FeatureOfUseCaseType(t) is not null)
            .ToList();

        // Act
        var violations = useCaseTypes
            .SelectMany(source => source.Dependencies
                .Select(d => d.Target)
                .Where(target => FeatureOfUseCaseType(target) is { } targetFeature
                    && targetFeature != FeatureOfUseCaseType(source)
                    && !target.Name.EndsWith("EndpointGroup", StringComparison.Ordinal))
                .Select(target => $"{source.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();

        // Assert
        useCaseTypes.ShouldNotBeEmpty();

        var newViolations = violations.Except(KnownSliceViolations).ToList();
        newViolations.ShouldBeEmpty(
            "New cross-slice dependencies (each use case defines its own DTO):" + Environment.NewLine + string.Join(Environment.NewLine, newViolations));

        var fixedViolations = KnownSliceViolations.Except(violations).Order().ToList();
        fixedViolations.ShouldBeEmpty(
            "Fixed dependencies still listed in KnownSliceViolations; remove them:" + Environment.NewLine + string.Join(Environment.NewLine, fixedViolations));
    }

    [Fact(DisplayName = "Feature domains should not depend on FastEndpoints")]
    public void FeatureDomains_ShouldNotDependOnFastEndpoints()
    {
        // Arrange
        var domainTypes = Architecture.Types
            .Where(t => FeatureOfDomainType(t) is not null)
            .ToList();

        // Act
        var violations = domainTypes
            .SelectMany(source => source.Dependencies
                .Select(d => d.Target)
                .Where(target => target.Namespace.FullName.StartsWith("FastEndpoints", StringComparison.Ordinal))
                .Select(target => $"{source.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();

        // Assert
        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact(DisplayName = "Validators should not depend on the database context")]
    public void Validators_ShouldNotDependOnDbContext()
    {
        // Arrange
        var validators = Architecture.Types
            .Where(t => t.Namespace.FullName.StartsWith("Neba.Api.Features.", StringComparison.Ordinal)
                && t.Name.EndsWith("Validator", StringComparison.Ordinal))
            .ToList();

        // Act
        var violations = validators
            .SelectMany(source => source.Dependencies
                .Select(d => d.Target)
                .Where(target => target.FullName is "Neba.Api.Database.AppDbContext" or "Neba.Api.Database.SecurityDbContext")
                .Select(target => $"{source.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();

        // Assert
        validators.ShouldNotBeEmpty();
        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact(DisplayName = "Only DomainEventPublisher should enqueue a domain event job")]
    public void OnlyDomainEventPublisher_ShouldEnqueueDomainEventJobs()
    {
        // Arrange
        const string publisher = "Neba.Api.BackgroundJobs.DomainEventPublisher";
        var types = Architecture.Types.ToList();

        static bool DependsOn(IType type, Func<IType, bool> match)
            => type.Dependencies.Any(d => match(d.Target));

        static bool IsEventJob(IType t) => t.FullName.StartsWith("Neba.Api.BackgroundJobs.IDomainEventJob", StringComparison.Ordinal);
        // A call that creates a job, not merely a reference to the client (registration code names both types).
        static bool CallsClient(IType type)
            => type.Members.SelectMany(m => m.MemberDependencies).OfType<MethodCallDependency>().Any(d =>
                d.TargetMember.DeclaringType.FullName is "Hangfire.IBackgroundJobClient" or "Hangfire.BackgroundJob"
                    or "Hangfire.BackgroundJobClient" or "Hangfire.BackgroundJobClientExtensions"
                && (d.TargetMember.Name.StartsWith("Enqueue", StringComparison.Ordinal)
                    || d.TargetMember.Name.StartsWith("Create", StringComparison.Ordinal)
                    || d.TargetMember.Name.StartsWith("Schedule", StringComparison.Ordinal)));

        // Act
        var enqueuers = types
            .Where(t => DependsOn(t, IsEventJob) && CallsClient(t))
            .ToList();

        var violations = enqueuers
            .Where(t => !t.FullName.StartsWith(publisher, StringComparison.Ordinal))
            .Select(t => t.FullName)
            .Order()
            .ToList();

        // Assert
        enqueuers.ShouldContain(t => t.FullName == publisher, "the publisher must be detected, or this test proves nothing");
        violations.ShouldBeEmpty(
            "Enqueue event jobs through IDomainEventPublisher:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact(DisplayName = "Every domain event type should have exactly one handler")]
    public void EveryDomainEvent_ShouldHaveExactlyOneHandler()
    {
        // Arrange
        var concreteTypes = typeof(AppDbContext).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var eventTypes = concreteTypes
            .Where(t => typeof(Neba.Api.Domain.IDomainEvent).IsAssignableFrom(t))
            .ToList();

        // Act
        var handlerCountByEvent = concreteTypes
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Neba.Api.BackgroundJobs.IDomainEventJob<>))
            .GroupBy(i => i.GetGenericArguments()[0])
            .ToDictionary(g => g.Key, g => g.Count());

        var violations = eventTypes
            .Select(e => (Event: e, Handlers: handlerCountByEvent.GetValueOrDefault(e)))
            .Where(x => x.Handlers != 1)
            .Select(x => $"{x.Event.FullName}: {x.Handlers} handlers")
            .Order()
            .ToList();

        // Assert
        eventTypes.ShouldNotBeEmpty("events must be detected, or this test proves nothing");
        violations.ShouldBeEmpty(
            "Each domain event needs exactly one IDomainEventJob<T>:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
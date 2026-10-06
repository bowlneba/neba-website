using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
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

    private static string? FeatureOfDomainType(IType type)
    {
        var match = FeatureDomainNamespace().Match(type.Namespace.FullName);
        return match.Success ? match.Groups["feature"].Value : null;
    }

    // A strongly-typed ID is a typed foreign key, not a domain dependency (see CLAUDE.md, Feature Boundaries).
    private static bool IsStronglyTypedId(IType type) => type.Name.EndsWith("Id", StringComparison.Ordinal);

    [Fact(DisplayName = "Feature domains should not depend on another feature's domain, except strongly-typed IDs")]
    public void FeatureDomains_ShouldNotDependOnOtherFeatureDomains()
    {
        // Arrange
        var domainTypes = Architecture.Types
            .Where(t => FeatureOfDomainType(t) is not null)
            .ToList();

        // Act
        var violations = domainTypes
            .SelectMany(source => source.Dependencies
                .Select(d => d.Target)
                .Where(target => FeatureOfDomainType(target) is { } targetFeature
                    && targetFeature != FeatureOfDomainType(source)
                    && !IsStronglyTypedId(target))
                .Select(target => $"{source.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();

        // Assert
        domainTypes.ShouldNotBeEmpty();

        var newViolations = violations.Except(KnownFeatureBoundaryViolations.All).ToList();
        newViolations.ShouldBeEmpty(
            "New cross-feature domain dependencies:" + Environment.NewLine + string.Join(Environment.NewLine, newViolations));

        var fixedViolations = KnownFeatureBoundaryViolations.All.Except(violations).Order().ToList();
        fixedViolations.ShouldBeEmpty(
            "Fixed dependencies still listed in KnownFeatureBoundaryViolations; remove them:" + Environment.NewLine + string.Join(Environment.NewLine, fixedViolations));
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
}

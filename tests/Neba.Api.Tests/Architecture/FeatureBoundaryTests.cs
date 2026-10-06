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
        "Neba.Api.Features.Tournaments.ListTournamentsInSeason.ListTournamentsInSeasonQueryHandler -> Neba.Api.Features.Seasons.ListSeasons.SeasonDto",
        "Neba.Api.Features.Tournaments.ListTournamentsInSeason.SeasonTournamentDto -> Neba.Api.Features.Seasons.ListSeasons.SeasonDto",
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
}
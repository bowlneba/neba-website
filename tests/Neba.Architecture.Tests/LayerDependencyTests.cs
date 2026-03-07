using ArchUnitNET.Domain;

using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class LayerDependencyTests : ArchitectureTestBase
{
    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain Layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure Layer");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("API Layer");

    private static readonly IObjectProvider<IType> ContractsLayer =
        Types().That().ResideInAssembly(ContractsAssembly).As("Contracts Layer");

    [Fact(DisplayName = "Domain layer should not depend on Application layer")]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
        => Types().That().Are(DomainLayer).Should().NotDependOnAny(ApplicationLayer).Check(ArchModel);

    [Fact(DisplayName = "Domain layer should not depend on Infrastructure layer")]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
        => Types().That().Are(DomainLayer).Should().NotDependOnAny(InfrastructureLayer).Check(ArchModel);

    [Fact(DisplayName = "Domain layer should not depend on API layer")]
    public void DomainLayer_ShouldNotDependOn_ApiLayer()
        => Types().That().Are(DomainLayer).Should().NotDependOnAny(ApiLayer).Check(ArchModel);

    [Fact(DisplayName = "Domain layer should not depend on Contracts layer")]
    public void DomainLayer_ShouldNotDependOn_ContractsLayer()
        => Types().That().Are(DomainLayer).Should().NotDependOnAny(ContractsLayer).Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on Infrastructure layer")]
    public void ApplicationLayer_ShouldNotDependOn_InfrastructureLayer()
        => Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InfrastructureLayer).Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on API layer")]
    public void ApplicationLayer_ShouldNotDependOn_ApiLayer()
        => Types().That().Are(ApplicationLayer).Should().NotDependOnAny(ApiLayer).Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on Contracts layer")]
    public void ApplicationLayer_ShouldNotDependOn_ContractsLayer()
        => Types().That().Are(ApplicationLayer).Should().NotDependOnAny(ContractsLayer).Check(ArchModel);

    [Fact(DisplayName = "Infrastructure layer should not depend on API layer")]
    public void InfrastructureLayer_ShouldNotDependOn_ApiLayer()
        => Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(ApiLayer).Check(ArchModel);

    [Fact(DisplayName = "Infrastructure layer should not depend on Contracts layer")]
    public void InfrastructureLayer_ShouldNotDependOn_ContractsLayer()
        => Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(ContractsLayer).Check(ArchModel);
}

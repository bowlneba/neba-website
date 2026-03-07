using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class DependencyGuardTests : ArchitectureTestBase
{
    [Fact(DisplayName = "Domain layer should not depend on Entity Framework Core")]
    public void DomainLayer_ShouldNotDependOn_EntityFrameworkCore()
        => Types().That().ResideInAssembly(DomainAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Microsoft.EntityFrameworkCore")
            .Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on Entity Framework Core")]
    public void ApplicationLayer_ShouldNotDependOn_EntityFrameworkCore()
        => Types().That().ResideInAssembly(ApplicationAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Microsoft.EntityFrameworkCore")
            .Check(ArchModel);

    [Fact(DisplayName = "Domain layer should not depend on Hangfire")]
    public void DomainLayer_ShouldNotDependOn_Hangfire()
        => Types().That().ResideInAssembly(DomainAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Hangfire")
            .Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on Hangfire")]
    public void ApplicationLayer_ShouldNotDependOn_Hangfire()
        => Types().That().ResideInAssembly(ApplicationAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Hangfire")
            .Check(ArchModel);

    [Fact(DisplayName = "Domain layer should not depend on Newtonsoft.Json")]
    public void DomainLayer_ShouldNotDependOn_NewtonsoftJson()
        => Types().That().ResideInAssembly(DomainAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Newtonsoft.Json")
            .Check(ArchModel);

    [Fact(DisplayName = "Application layer should not depend on Newtonsoft.Json")]
    public void ApplicationLayer_ShouldNotDependOn_NewtonsoftJson()
        => Types().That().ResideInAssembly(ApplicationAssembly).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace("Newtonsoft.Json")
            .Check(ArchModel);
}

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using Neba.Api;
using Neba.Api.Contracts;
using Neba.Application;
using Neba.Domain;
using Neba.Infrastructure;

using SystemAssembly = System.Reflection.Assembly;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Neba.Architecture.Tests;

public abstract class ArchitectureTestBase
{
    protected static readonly SystemAssembly DomainAssembly = typeof(AggregateRoot).Assembly;
    protected static readonly SystemAssembly ApplicationAssembly = typeof(IApplicationAssemblyMarker).Assembly;
    protected static readonly SystemAssembly InfrastructureAssembly = typeof(IInfrastructureAssemblyMarker).Assembly;
    protected static readonly SystemAssembly ApiAssembly = typeof(IApiAssemblyMarker).Assembly;
    protected static readonly SystemAssembly ContractsAssembly = typeof(ICollectionResponse<>).Assembly;

    protected static readonly ArchitectureModel ArchModel = new ArchLoader()
        .LoadAssemblies(
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            ApiAssembly,
            ContractsAssembly)
        .Build();
}

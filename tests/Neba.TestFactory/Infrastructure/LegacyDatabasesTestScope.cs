namespace Neba.TestFactory.Infrastructure;

// A test class can only belong to one xunit collection. Legacy-connection integration tests need
// both the website's AppDbContextFixture (Postgres) and LegacySqlServerFixture (SQL Server,
// standing in for neba-fwk) shared once across the whole run rather than re-provisioned per test
// class, so this collection combines both instead of each test class picking one or the other.
//
// The [Collection] users live in Neba.Api.Tests, a different assembly, so xUnit1073 (which only
// looks inside the defining assembly) wrongly reports the definition as unused.
#pragma warning disable xUnit1073
[CollectionDefinition(nameof(LegacyDatabasesTestScope))]
public sealed class LegacyDatabasesTestScope : ICollectionFixture<AppDbContextFixture>, ICollectionFixture<LegacySqlServerFixture>;
#pragma warning restore xUnit1073

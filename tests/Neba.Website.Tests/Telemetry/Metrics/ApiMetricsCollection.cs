using Neba.TestFactory.Attributes;

namespace Neba.Website.Tests.Telemetry.Metrics;

[CollectionDefinition("ApiMetrics", DisableParallelization = true)]
[UnitTest]
public sealed class ApiMetricsTestScope;
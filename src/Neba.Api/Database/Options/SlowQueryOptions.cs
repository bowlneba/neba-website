namespace Neba.Api.Database.Options;

internal sealed class SlowQueryOptions
{
    public const string SectionName = "SlowQuery";

    public int ThresholdMs { get; set; } = 1000;
}
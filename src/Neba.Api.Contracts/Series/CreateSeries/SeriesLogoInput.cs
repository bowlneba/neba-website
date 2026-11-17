namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Series logo input.</summary>
public sealed record SeriesLogoInput
{
    /// <summary>The container.</summary>
    public required string Container { get; init; }
    /// <summary>The path.</summary>
    public required string Path { get; init; }
    /// <summary>The content type.</summary>
    public required string ContentType { get; init; }
    /// <summary>The size in bytes.</summary>
    public required long SizeInBytes { get; init; }
}

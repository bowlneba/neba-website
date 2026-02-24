using Neba.Application.Caching;

namespace Neba.TestFactory.Caching;

public static class CacheDescriptorFactory
{
    public static CacheDescriptor Create(
        string? key = null,
        IReadOnlyCollection<string>? tags = null)
        => new()
        {
            Key = key ?? $"test:key:{Guid.NewGuid()}",
            Tags = tags ?? [$"test:tag:{Guid.NewGuid()}"]
        };
}
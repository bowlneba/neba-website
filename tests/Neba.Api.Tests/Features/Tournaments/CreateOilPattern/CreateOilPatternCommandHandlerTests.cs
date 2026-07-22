using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.CreateOilPattern;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.CreateOilPattern;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class CreateOilPatternCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private CreateOilPatternCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return new CreateOilPatternCommandHandler(_dbContext, cache);
    }

    private static CreateOilPatternCommand ValidCommand(
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null)
        => new()
        {
            Name = name ?? OilPatternFactory.ValidName,
            Length = length ?? OilPatternFactory.ValidLength,
            Volume = volume ?? OilPatternFactory.ValidVolume,
            LeftRatio = leftRatio ?? OilPatternFactory.ValidLeftRatio,
            RightRatio = rightRatio ?? OilPatternFactory.ValidRightRatio,
            KegelId = kegelId
        };

    [Fact(DisplayName = "HandleAsync returns a conflict error when the Kegel id already exists")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenKegelIdAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var kegelId = Guid.NewGuid();
        var existing = OilPatternFactory.Create(kegelId: kegelId);
        await _dbContext.OilPatterns.AddAsync(existing, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(kegelId: kegelId);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OilPattern.KegelId.AlreadyExists");
    }

    [Fact(DisplayName = "HandleAsync does not persist an oil pattern when the Kegel id already exists")]
    public async Task HandleAsync_ShouldNotPersistOilPattern_WhenKegelIdAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var kegelId = Guid.NewGuid();
        var existing = OilPatternFactory.Create(kegelId: kegelId);
        await _dbContext.OilPatterns.AddAsync(existing, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Duplicate Kegel Pattern", kegelId: kegelId);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.OilPatterns.CountAsync(ct);
        count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync succeeds when the Kegel id is null")]
    public async Task HandleAsync_ShouldSucceed_WhenKegelIdIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(kegelId: null);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when oil pattern creation fails")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenOilPatternCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OilPattern.Name.Required");
    }

    [Fact(DisplayName = "HandleAsync does not persist an oil pattern when creation fails")]
    public async Task HandleAsync_ShouldNotPersistOilPattern_WhenOilPatternCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.OilPatterns.CountAsync(ct);
        count.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync persists the oil pattern when the command is valid")]
    public async Task HandleAsync_ShouldPersistOilPattern_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: "My New Pattern", length: 42, volume: 22.5m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.OilPatterns.AsNoTracking()
            .SingleAsync(p => p.Name == "My New Pattern", ct);
        persisted.Length.ShouldBe(42);
        persisted.Volume.ShouldBe(22.5m);
    }

    [Fact(DisplayName = "HandleAsync returns the created oil pattern's details when the command is valid")]
    public async Task HandleAsync_ShouldReturnCreatedOilPattern_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: "Returned Pattern", length: 41, leftRatio: 2m, rightRatio: 2m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.OilPatterns.AsNoTracking()
            .SingleAsync(p => p.Name == "Returned Pattern", ct);
        result.Value.Id.ShouldBe(persisted.Id);
        result.Value.Name.ShouldBe(persisted.Name);
        result.Value.Length.ShouldBe(persisted.Length);
        result.Value.LengthCategory.ShouldBe(persisted.LengthCategory);
        result.Value.RatioCategory.ShouldBe(persisted.RatioCategory);
    }

    [Fact(DisplayName = "HandleAsync invalidates the oil patterns cache tag when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateOilPatternsCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:oil-patterns:list";
        const string cacheTag = "neba:oil-patterns";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Cache Invalidation Pattern");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache tag when oil pattern creation fails")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenOilPatternCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:oil-patterns:list";
        const string cacheTag = "neba:oil-patterns";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was created
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("cached-list");
    }
}

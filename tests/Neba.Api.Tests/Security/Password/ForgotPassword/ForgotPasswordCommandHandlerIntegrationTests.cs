using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Security.Domain;
using Neba.Api.Security.Password.ForgotPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Security.Password.ForgotPassword;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ForgotPasswordCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private readonly FakeLogger<ForgotPasswordCommandHandler> _logger = new();
    private readonly FusionCache _cache = new(new FusionCacheOptions());
    private readonly Mock<IBackgroundJobClient> _jobs = new(MockBehavior.Strict);
    private readonly List<Job> _enqueued = [];
    private readonly AutoAdvanceTimeProvider _timeProvider = new();

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        _jobs
            .Setup(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => _enqueued.Add(job))
            .Returns("1");
    }

    public ValueTask DisposeAsync()
    {
        _cache.Dispose();
        return ValueTask.CompletedTask;
    }

    private ForgotPasswordCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
        => new(userManager, _cache, _jobs.Object, _timeProvider, _logger);

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    [Fact(DisplayName = "HandleAsync enqueues one reset email job for a known email")]
    public async Task HandleAsync_ShouldEnqueueJob_WhenEmailIsKnown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new ForgotPasswordCommand { Email = LoginRequestFactory.ValidEmail };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var job = _enqueued.ShouldHaveSingleItem();
        job.Type.ShouldBe(typeof(SendPasswordResetEmailJob));
        job.Args[0].ShouldBe(user.Id.ToString());
        _logger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns success and enqueues nothing for an unknown email")]
    public async Task HandleAsync_ShouldReturnSuccessAndEnqueueNothing_WhenEmailIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new ForgotPasswordCommand { Email = "nobody@example.com" };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _enqueued.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync logs a warning with the email, hash, domain, IP, and user agent for an unknown email")]
    public async Task HandleAsync_ShouldLogWarningWithDetails_WhenEmailIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new ForgotPasswordCommand
        {
            Email = "Nobody@Example.com",
            RequesterIp = "203.0.113.7",
            UserAgent = "TestAgent/1.0"
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("Nobody@Example.com");
        record.Message.ShouldContain("EXAMPLE.COM");
        record.Message.ShouldContain("203.0.113.7");
        record.Message.ShouldContain("TestAgent/1.0");
        record.StructuredState!.ShouldContain(pair => pair.Key == "EmailHash" && pair.Value!.Length == 12);
    }

    [Fact(DisplayName = "HandleAsync gives the same hash for the same email in a different case")]
    public async Task HandleAsync_ShouldLogSameHash_ForSameEmailInDifferentCase()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var handler = CreateHandler(userManager);

        // Act
        await handler.HandleAsync(new ForgotPasswordCommand { Email = "Nobody@Example.com" }, ct);
        await handler.HandleAsync(new ForgotPasswordCommand { Email = " nobody@example.com " }, ct);

        // Assert
        var hashes = _logger.Collector.GetSnapshot()
            .Select(r => r.StructuredState!.Single(pair => pair.Key == "EmailHash").Value)
            .ToList();
        hashes.Count.ShouldBe(2);
        hashes[0].ShouldBe(hashes[1]);
    }

    [Fact(DisplayName = "HandleAsync strips control characters and caps the length of the user agent in the log")]
    public async Task HandleAsync_ShouldSanitizeUserAgent_WhenLogging()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new ForgotPasswordCommand
        {
            Email = "nobody@example.com",
            UserAgent = "Agent\r\nFAKE LOG LINE" + new string('x', 500)
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var userAgent = _logger.Collector.GetSnapshot().ShouldHaveSingleItem()
            .StructuredState!.Single(pair => pair.Key == "UserAgent").Value!;
        userAgent.ShouldNotContain("\r");
        userAgent.ShouldNotContain("\n");
        userAgent.Length.ShouldBe(256);
    }

    [Fact(DisplayName = "HandleAsync logs unknown for a missing IP and user agent")]
    public async Task HandleAsync_ShouldLogUnknown_WhenIpAndUserAgentAreMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new ForgotPasswordCommand { Email = "nobody@example.com" };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var state = _logger.Collector.GetSnapshot().ShouldHaveSingleItem().StructuredState!;
        state.Single(pair => pair.Key == "ClientIp").Value.ShouldBe("unknown");
        state.Single(pair => pair.Key == "UserAgent").Value.ShouldBe("unknown");
    }

    [Fact(DisplayName = "HandleAsync logs unknown for the domain when the email has no at sign")]
    public async Task HandleAsync_ShouldLogUnknownDomain_WhenEmailHasNoAtSign()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new ForgotPasswordCommand { Email = "no-at-sign" };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem()
            .StructuredState!.Single(pair => pair.Key == "EmailDomain").Value.ShouldBe("unknown");
    }

    [Fact(DisplayName = "HandleAsync enqueues nothing for a second request inside the cooldown")]
    public async Task HandleAsync_ShouldEnqueueOnce_WhenRequestedTwiceInsideCooldown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var handler = CreateHandler(userManager);
        var command = new ForgotPasswordCommand { Email = LoginRequestFactory.ValidEmail };

        // Act
        var first = await handler.HandleAsync(command, ct);
        var second = await handler.HandleAsync(command, ct);

        // Assert
        first.IsError.ShouldBeFalse();
        second.IsError.ShouldBeFalse();
        _enqueued.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync enqueues one job when concurrent requests arrive for the same account")]
    public async Task HandleAsync_ShouldEnqueueOnce_WhenRequestsAreConcurrent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var seedScope = fixture.CreateScope();
        await SeedUserAsync(seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
        var command = new ForgotPasswordCommand { Email = LoginRequestFactory.ValidEmail };

        // Act — one scope per request, as in production, so each has its own DbContext
        await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            using var scope = fixture.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await CreateHandler(userManager).HandleAsync(command, ct);
        }));

        // Assert
        _enqueued.Count.ShouldBe(1);
    }

    [Theory(DisplayName = "HandleAsync takes at least the minimum duration whether or not the account exists")]
    [InlineData(true, TestDisplayName = "Known email")]
    [InlineData(false, TestDisplayName = "Unknown email")]
    public async Task HandleAsync_ShouldTakeMinimumDuration_RegardlessOfAccount(bool known)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (known)
        {
            await SeedUserAsync(userManager);
        }

        var command = new ForgotPasswordCommand { Email = LoginRequestFactory.ValidEmail };
        var start = _timeProvider.GetUtcNow();

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        (_timeProvider.GetUtcNow() - start).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(300));
    }
}

/// <summary>
/// A fake clock whose timers fire as soon as they are created, so a handler that waits does not slow the test.
/// </summary>
internal sealed class AutoAdvanceTimeProvider : FakeTimeProvider
{
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        ThreadPool.QueueUserWorkItem(_ => Advance(dueTime));
        return timer;
    }
}

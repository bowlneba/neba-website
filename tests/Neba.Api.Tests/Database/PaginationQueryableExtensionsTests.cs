using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Database;

[IntegrationTest]
[Component("Infrastructure")]
[Collection<SecurityDbContextFixture>]
public sealed class PaginationQueryableExtensionsTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private sealed record PageQuery(int Page, int PageSize) : IPaginationQuery
    {
        public int Page { get; init; } = Page;
        public int PageSize { get; init; } = PageSize;
    }

    private async Task SeedUsersAsync(params string[] emails)
    {
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var email in emails)
        {
            await userManager.CreateAsync(ApplicationUserFactory.Create(email: email, userName: email));
        }
    }

    [Fact(DisplayName = "ApplyPagination translates Skip/Take against the real provider and returns the correct page")]
    public async Task ApplyPagination_ShouldReturnCorrectPage_WhenTranslatedByProvider()
    {
        // Arrange
        await SeedUsersAsync("a@bowlneba.com", "b@bowlneba.com", "c@bowlneba.com", "d@bowlneba.com", "e@bowlneba.com");
        await using var dbContext = fixture.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var page2 = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ApplyPagination(new PageQuery(2, 2))
            .Select(user => user.Email)
            .ToListAsync(ct);

        // Assert
        page2.ShouldBe(["c@bowlneba.com", "d@bowlneba.com"]);
    }

    [Fact(DisplayName = "ApplyPagination returns non-overlapping, exhaustive pages across the full result set")]
    public async Task ApplyPagination_ShouldReturnNonOverlappingPages_AcrossFullResultSet()
    {
        // Arrange
        await SeedUsersAsync("a@bowlneba.com", "b@bowlneba.com", "c@bowlneba.com", "d@bowlneba.com", "e@bowlneba.com");
        await using var dbContext = fixture.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var page1 = await dbContext.Users.AsNoTracking().OrderBy(u => u.Email).ApplyPagination(new PageQuery(1, 2)).Select(u => u.Email).ToListAsync(ct);
        var page2 = await dbContext.Users.AsNoTracking().OrderBy(u => u.Email).ApplyPagination(new PageQuery(2, 2)).Select(u => u.Email).ToListAsync(ct);
        var page3 = await dbContext.Users.AsNoTracking().OrderBy(u => u.Email).ApplyPagination(new PageQuery(3, 2)).Select(u => u.Email).ToListAsync(ct);

        // Assert
        page1.ShouldBe(["a@bowlneba.com", "b@bowlneba.com"]);
        page2.ShouldBe(["c@bowlneba.com", "d@bowlneba.com"]);
        page3.ShouldBe(["e@bowlneba.com"]);
        page1.Concat(page2).Concat(page3).Distinct().Count().ShouldBe(5);
    }

    [Fact(DisplayName = "ApplyPagination returns an empty page when the requested page is past the end of the result set")]
    public async Task ApplyPagination_ShouldReturnEmpty_WhenPageIsPastEndOfResultSet()
    {
        // Arrange
        await SeedUsersAsync("a@bowlneba.com", "b@bowlneba.com");
        await using var dbContext = fixture.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ApplyPagination(new PageQuery(5, 2))
            .ToListAsync(ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyPagination returns the entire result set when page size exceeds the total item count")]
    public async Task ApplyPagination_ShouldReturnEntireResultSet_WhenPageSizeExceedsTotalItemCount()
    {
        // Arrange
        await SeedUsersAsync("a@bowlneba.com", "b@bowlneba.com", "c@bowlneba.com");
        await using var dbContext = fixture.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ApplyPagination(new PageQuery(1, 100))
            .Select(user => user.Email)
            .ToListAsync(ct);

        // Assert
        result.ShouldBe(["a@bowlneba.com", "b@bowlneba.com", "c@bowlneba.com"]);
    }
}

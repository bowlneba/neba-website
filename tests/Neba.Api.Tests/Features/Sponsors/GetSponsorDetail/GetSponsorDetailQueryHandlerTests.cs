using Neba.Api.Database;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Sponsors.GetSponsorDetail;

[IntegrationTest]
[Component("Sponsors")]
[Collection<PostgreSqlFixture>]
public sealed class GetSponsorDetailQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "HandleAsync returns SponsorNotFound when no sponsor matches the slug")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSlugDoesNotExist()
    {
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetSponsorDetailQuery { Slug = "nonexistent-sponsor" },
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns sponsor detail with correct fields when slug matches")]
    public async Task HandleAsync_ShouldReturnSponsor_WithCorrectFields_WhenSlugMatches()
    {
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(
            name: "ACME Corp",
            slug: "acme-corp",
            isCurrentSponsor: true,
            priority: 2);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetSponsorDetailQuery { Slug = "acme-corp" }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("ACME Corp");
        result.Value.Slug.ShouldBe("acme-corp");
        result.Value.IsCurrentSponsor.ShouldBeTrue();
        result.Value.Priority.ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when sponsor has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenSponsorHasLogo()
    {
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "logos", path: "sponsors/acme-logo.png");
        var sponsor = SponsorFactory.Create(slug: "logo-sponsor", logo: logo);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/logos/sponsors/acme-logo.png");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("logos", "sponsors/acme-logo.png"))
            .Returns(expectedUri);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetSponsorDetailQuery { Slug = "logo-sponsor" }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync returns NotFound when a different slug is queried")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenDifferentSlugQueried()
    {
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(slug: "real-sponsor");
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetSponsorDetailQuery { Slug = "other-sponsor" }, ct);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
    }
}
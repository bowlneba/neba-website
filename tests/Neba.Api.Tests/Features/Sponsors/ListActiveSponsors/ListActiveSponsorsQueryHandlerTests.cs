using Neba.Api.Database;
using Neba.Api.Features.Sponsors.ListActiveSponsors;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Sponsors.ListActiveSponsors;

[IntegrationTest]
[Component("Sponsors")]
[Collection<PostgreSqlFixture>]
public sealed class ListActiveSponsorsQueryHandlerTests(PostgreSqlFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns empty collection when no active sponsors exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoActiveSponsorsExist()
    {
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListActiveSponsorsQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new ListActiveSponsorsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync excludes inactive sponsors")]
    public async Task HandleAsync_ShouldExcludeInactiveSponsors_WhenOnlyInactiveExist()
    {
        var ct = TestContext.Current.CancellationToken;
        var inactive = SponsorFactory.Create(isCurrentSponsor: false, slug: "inactive-co");
        await _dbContext.Sponsors.AddAsync(inactive, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListActiveSponsorsQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(new ListActiveSponsorsQuery(), ct);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns active sponsor with correct fields")]
    public async Task HandleAsync_ShouldReturnActiveSponsor_WithCorrectFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(
            name: "ACME Corp",
            slug: "acme-corp",
            isCurrentSponsor: true,
            priority: 1);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListActiveSponsorsQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(new ListActiveSponsorsQuery(), ct);

        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Name.ShouldBe("ACME Corp");
        dto.Slug.ShouldBe("acme-corp");
        dto.IsCurrentSponsor.ShouldBeTrue();
        dto.Priority.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when sponsor has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenSponsorHasLogo()
    {
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "logos", path: "sponsors/acme-logo.png");
        var sponsor = SponsorFactory.Create(isCurrentSponsor: true, slug: "logo-sponsor", logo: logo);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/logos/sponsors/acme-logo.png");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("logos", "sponsors/acme-logo.png"))
            .Returns(expectedUri);
        var handler = new ListActiveSponsorsQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(new ListActiveSponsorsQuery(), ct);

        result.ShouldHaveSingleItem();
        result.Single().LogoUrl.ShouldBe(expectedUri);
    }
}

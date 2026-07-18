using Neba.Api.Contacts.Domain;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Sponsors.GetSponsorDetail;

[IntegrationTest]
[Component("Sponsors")]
[Collection<AppDbContextFixture>]
public sealed class GetSponsorDetailQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private static GetSponsorDetailQuery QueryFor(string slug, bool callerHasSponsorManagementPermission = false) =>
        new() { Slug = slug, CallerHasSponsorManagementPermission = callerHasSponsorManagementPermission };

    [Fact(DisplayName = "HandleAsync returns SponsorNotFound when no sponsor matches the slug")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSlugDoesNotExist()
    {
        // Arrange
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            QueryFor("nonexistent-sponsor"),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns sponsor detail with correct fields when slug matches")]
    public async Task HandleAsync_ShouldReturnSponsor_WithCorrectFields_WhenSlugMatches()
    {
        // Arrange
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

        // Act
        var result = await handler.HandleAsync(QueryFor("acme-corp"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("ACME Corp");
        result.Value.Slug.ShouldBe("acme-corp");
        result.Value.IsCurrentSponsor.ShouldBeTrue();
        result.Value.Priority.ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when sponsor has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenSponsorHasLogo()
    {
        // Arrange
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

        // Act
        var result = await handler.HandleAsync(QueryFor("logo-sponsor"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync returns NotFound when a different slug is queried")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenDifferentSlugQueried()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(slug: "real-sponsor");
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("other-sponsor"), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns SponsorNotFound for an inactive sponsor when caller lacks sponsor management permission")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSponsorIsInactiveAndCallerLacksManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(slug: "inactive-sponsor", isCurrentSponsor: false);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            QueryFor("inactive-sponsor", callerHasSponsorManagementPermission: false), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns sponsor detail for an inactive sponsor when caller has sponsor management permission")]
    public async Task HandleAsync_ShouldReturnSponsor_WhenSponsorIsInactiveAndCallerHasManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sponsor = SponsorFactory.Create(name: "Inactive Co", slug: "inactive-sponsor", isCurrentSponsor: false);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            QueryFor("inactive-sponsor", callerHasSponsorManagementPermission: true), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("Inactive Co");
        result.Value.IsCurrentSponsor.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync populates LiveReadText, PromotionalNotes, and Contact when caller has sponsor management permission")]
    public async Task HandleAsync_ShouldPopulateAdminOnlyFields_WhenCallerHasManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var contact = ContactInfoFactory.Create(
            name: "Jane Doe",
            phone: PhoneNumberFactory.Create(),
            email: EmailAddressFactory.Create("jane@example.com"));
        var sponsor = SponsorFactory.Create(
            slug: "admin-only-fields",
            liveReadText: "Read this live",
            promotionalNotes: "Internal notes",
            sponsorContact: contact);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            QueryFor("admin-only-fields", callerHasSponsorManagementPermission: true), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LiveReadText.ShouldBe("Read this live");
        result.Value.PromotionalNotes.ShouldBe("Internal notes");
        result.Value.Contact.ShouldNotBeNull();
        result.Value.Contact.Name.ShouldBe("Jane Doe");
        result.Value.Contact.Email.ShouldBe("jane@example.com");
    }

    [Fact(DisplayName = "HandleAsync suppresses LiveReadText, PromotionalNotes, and Contact when caller lacks sponsor management permission")]
    public async Task HandleAsync_ShouldSuppressAdminOnlyFields_WhenCallerLacksManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var contact = ContactInfoFactory.Create(name: "Jane Doe");
        var sponsor = SponsorFactory.Create(
            slug: "public-view-fields",
            liveReadText: "Read this live",
            promotionalNotes: "Internal notes",
            sponsorContact: contact);
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetSponsorDetailQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            QueryFor("public-view-fields", callerHasSponsorManagementPermission: false), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LiveReadText.ShouldBeNull();
        result.Value.PromotionalNotes.ShouldBeNull();
        result.Value.Contact.ShouldBeNull();
    }
}
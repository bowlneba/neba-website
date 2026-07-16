using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Sponsors;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Sponsors.CreateSponsor;

[IntegrationTest]
[Component("Sponsors")]
[Collection<AppDbContextFixture>]
public sealed class CreateSponsorCommandHandlerTests(AppDbContextFixture fixture)
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

    private CreateSponsorCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return new CreateSponsorCommandHandler(_dbContext, cache);
    }

    private static CreateSponsorCommand ValidCommand(
        string? name = null,
        string? slug = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessUnit = null,
        string? businessCity = null,
        UsState? businessState = null,
        string? businessPostalCode = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<PhoneNumberInput>? phoneNumbers = null,
        string? contactName = null,
        PhoneNumberType? contactPhoneType = null,
        string? contactPhoneNumber = null,
        string? contactPhoneExtension = null,
        string? contactEmail = null)
        => new()
        {
            Name = name ?? SponsorFactory.ValidName,
            Slug = slug,
            IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
            Priority = priority ?? SponsorFactory.ValidPriority,
            Tier = tier ?? SponsorFactory.ValidTier,
            Category = category ?? SponsorFactory.ValidCategory,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
            Description = description,
            LiveReadText = liveReadText,
            PromotionalNotes = promotionalNotes,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl,
            BusinessStreet = businessStreet,
            BusinessUnit = businessUnit,
            BusinessCity = businessCity,
            BusinessState = businessState,
            BusinessPostalCode = businessPostalCode,
            BusinessEmailAddress = businessEmailAddress,
            PhoneNumbers = phoneNumbers ?? [],
            ContactName = contactName,
            ContactPhoneType = contactPhoneType,
            ContactPhoneNumber = contactPhoneNumber,
            ContactPhoneExtension = contactPhoneExtension,
            ContactEmail = contactEmail
        };

    [Fact(DisplayName = "HandleAsync returns validation error when sponsor creation fails")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSponsorCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.Name.Required");
    }

    [Fact(DisplayName = "HandleAsync does not persist a sponsor when sponsor creation fails")]
    public async Task HandleAsync_ShouldNotPersistSponsor_WhenSponsorCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Sponsors.CountAsync(ct);
        count.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync returns Conflict error when slug already exists")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenSlugAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingSponsor = SponsorFactory.Create(slug: "existing-slug");
        await _dbContext.Sponsors.AddAsync(existingSponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Existing Slug", slug: "existing-slug");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Sponsor.Slug.AlreadyExists");
    }

    [Fact(DisplayName = "HandleAsync does not persist a duplicate sponsor when slug already exists")]
    public async Task HandleAsync_ShouldNotPersistDuplicateSponsor_WhenSlugAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingSponsor = SponsorFactory.Create(slug: "duplicate-slug");
        await _dbContext.Sponsors.AddAsync(existingSponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Duplicate Slug", slug: "duplicate-slug");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Sponsors.CountAsync(s => s.Slug == "duplicate-slug", ct);
        count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync returns Conflict error when tier is TitleSponsor and title sponsorship is already taken")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenTitleSponsorshipAlreadyTaken()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingTitleSponsor = SponsorFactory.Create(
            slug: "current-title-sponsor",
            isCurrentSponsor: true,
            tier: SponsorTier.TitleSponsor);
        await _dbContext.Sponsors.AddAsync(existingTitleSponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "new-title-sponsor", tier: SponsorTier.TitleSponsor);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Sponsor.TitleSponsor.Unavailable");
    }

    [Fact(DisplayName = "HandleAsync succeeds when tier is TitleSponsor and the existing title sponsor is not current")]
    public async Task HandleAsync_ShouldSucceed_WhenExistingTitleSponsorIsNotCurrent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var pastTitleSponsor = SponsorFactory.Create(
            slug: "past-title-sponsor",
            isCurrentSponsor: false,
            tier: SponsorTier.TitleSponsor);
        await _dbContext.Sponsors.AddAsync(pastTitleSponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "available-title-sponsor", tier: SponsorTier.TitleSponsor);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the business address is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBusinessAddressIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            businessStreet: AddressFactory.ValidStreet,
            businessCity: string.Empty,
            businessState: AddressFactory.ValidUsState,
            businessPostalCode: AddressFactory.ValidZipCode);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "HandleAsync persists the business address when provided")]
    public async Task HandleAsync_ShouldPersistBusinessAddress_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            slug: "sponsor-with-address",
            businessStreet: AddressFactory.ValidStreet,
            businessUnit: AddressFactory.ValidUnit,
            businessCity: AddressFactory.ValidCity,
            businessState: AddressFactory.ValidUsState,
            businessPostalCode: AddressFactory.ValidZipCode);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-with-address", ct);
        persisted.BusinessAddress.ShouldNotBeNull();
        persisted.BusinessAddress.Street.ShouldBe(AddressFactory.ValidStreet);
        persisted.BusinessAddress.City.ShouldBe(AddressFactory.ValidCity);
    }

    [Fact(DisplayName = "HandleAsync does not build a business address when the street is not provided")]
    public async Task HandleAsync_ShouldNotBuildBusinessAddress_WhenStreetNotProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(slug: "sponsor-without-address");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-without-address", ct);
        persisted.BusinessAddress.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the business email is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBusinessEmailIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(businessEmailAddress: "not-an-email");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.InvalidEmailAddress");
    }

    [Fact(DisplayName = "HandleAsync persists the business email when provided")]
    public async Task HandleAsync_ShouldPersistBusinessEmail_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(slug: "sponsor-with-email", businessEmailAddress: EmailAddressFactory.ValidEmail);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-with-email", ct);
        persisted.BusinessEmail.ShouldNotBeNull();
        persisted.BusinessEmail.Value.ShouldBe(EmailAddressFactory.ValidEmail);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when a phone number is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenPhoneNumberIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(phoneNumbers:
        [
            new PhoneNumberInput { Type = PhoneNumberType.Work, Number = string.Empty }
        ]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    [Fact(DisplayName = "HandleAsync persists phone numbers when provided")]
    public async Task HandleAsync_ShouldPersistPhoneNumbers_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(slug: "sponsor-with-phone", phoneNumbers:
        [
            new PhoneNumberInput { Type = PhoneNumberType.Work, Number = PhoneNumberFactory.ValidNumber }
        ]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-with-phone", ct);
        var phoneNumber = persisted.PhoneNumbers.ShouldHaveSingleItem();
        phoneNumber.Type.ShouldBe(PhoneNumberType.Work);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the sponsor contact is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSponsorContactIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: PhoneNumberType.Mobile,
            contactPhoneNumber: string.Empty,
            contactEmail: EmailAddressFactory.ValidEmail);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    [Fact(DisplayName = "HandleAsync persists the sponsor contact when provided")]
    public async Task HandleAsync_ShouldPersistSponsorContact_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            slug: "sponsor-with-contact",
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: PhoneNumberType.Mobile,
            contactPhoneNumber: PhoneNumberFactory.ValidNumber,
            contactEmail: EmailAddressFactory.ValidEmail);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-with-contact", ct);
        persisted.SponsorContact.ShouldNotBeNull();
        persisted.SponsorContact.Name.ShouldBe(ContactInfoFactory.ValidName);
        persisted.SponsorContact.Email.Value.ShouldBe(EmailAddressFactory.ValidEmail);
    }

    [Fact(DisplayName = "HandleAsync does not build a sponsor contact when none of the contact fields are supplied")]
    public async Task HandleAsync_ShouldNotBuildSponsorContact_WhenNoContactFieldsSupplied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(slug: "sponsor-without-contact");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "sponsor-without-contact", ct);
        persisted.SponsorContact.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync persists the sponsor when command is valid")]
    public async Task HandleAsync_ShouldPersistSponsor_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            name: "My New Sponsor",
            slug: "my-new-sponsor",
            isCurrentSponsor: true,
            priority: 5,
            tier: SponsorTier.Standard,
            category: SponsorCategory.Technology);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "my-new-sponsor", ct);
        persisted.Name.ShouldBe("My New Sponsor");
        persisted.IsCurrentSponsor.ShouldBeTrue();
        persisted.Priority.ShouldBe(5);
        persisted.Tier.ShouldBe(SponsorTier.Standard);
        persisted.Category.ShouldBe(SponsorCategory.Technology);
    }

    [Fact(DisplayName = "HandleAsync returns the created sponsor's id and normalized slug when command is valid")]
    public async Task HandleAsync_ShouldReturnCreatedSponsorIdAndSlug_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: "Some Sponsor", slug: "Some Slug!!");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("some-slug");
        var persisted = await _dbContext.Sponsors.AsNoTracking()
            .SingleAsync(s => s.Slug == "some-slug", ct);
        result.Value.Id.ShouldBe(persisted.Id);
    }

    [Fact(DisplayName = "HandleAsync generates the slug from the name when slug is not provided")]
    public async Task HandleAsync_ShouldGenerateSlugFromName_WhenSlugIsNotProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(name: "A Brand New Sponsor", slug: null);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("a-brand-new-sponsor");
    }

    [Fact(DisplayName = "HandleAsync invalidates the sponsors cache tag when command is valid")]
    public async Task HandleAsync_ShouldInvalidateSponsorsCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:sponsors:list";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: ["neba:sponsors"],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "cache-invalidation-create-sponsor");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the sponsors cache tag when sponsor creation fails")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenSponsorCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:sponsors:list";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: ["neba:sponsors"],
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

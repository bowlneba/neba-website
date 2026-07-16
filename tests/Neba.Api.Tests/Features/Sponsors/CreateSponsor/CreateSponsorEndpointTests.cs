using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors;
using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Sponsors.CreateSponsor;

[UnitTest]
[Component("Sponsors")]
public sealed class CreateSponsorEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenCreationSucceeds()
    {
        // Arrange
        var input = SponsorInputFactory.Create(name: "Storm Products Inc.", slug: "storm-products-inc");
        var request = new CreateSponsorRequest { Sponsor = input };
        var ct = TestContext.Current.CancellationToken;
        var createdSponsor = CreatedSponsorFactory.Create(slug: "storm-products-inc");

        CreateSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateSponsorCommand>(), ct))
            .Callback<CreateSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(createdSponsor);

        var endpoint = Factory.Create<CreateSponsorEndpoint>(commandHandlerMock.Object);

        // Act — Send.CreatedAtAsync requires LinkGenerator, which Factory.Create does not provide.
        // The strict mock verifies the command mapping; the LinkGenerator exception confirms the success branch was taken.
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        exception.Message.ShouldContain("LinkGenerator");
        capturedCommand.ShouldNotBeNull();
        capturedCommand.Name.ShouldBe(input.Name);
        capturedCommand.Slug.ShouldBe(input.Slug);
        capturedCommand.IsCurrentSponsor.ShouldBe(input.IsCurrentSponsor);
        capturedCommand.Priority.ShouldBe(input.Priority);
        capturedCommand.Tier.ShouldBe(SponsorTier.FromName(input.Tier));
        capturedCommand.Category.ShouldBe(SponsorCategory.FromName(input.Category));
        capturedCommand.Logo.ShouldBeNull();
        capturedCommand.BusinessState.ShouldBeNull();
        capturedCommand.PhoneNumbers.ShouldBeEmpty();
        capturedCommand.ContactName.ShouldBeNull();
        capturedCommand.ContactPhoneType.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map a supplied logo, business address, phone numbers, and contact onto the command")]
    public async Task HandleAsync_ShouldMapLogoAddressPhoneNumbersAndContact_WhenSupplied()
    {
        // Arrange
        var logo = new SponsorLogoInput
        {
            Container = "sponsor-logos",
            Path = "storm/logo.png",
            ContentType = "image/png",
            SizeInBytes = 2048
        };
        var phoneNumber = new SponsorPhoneNumberInput
        {
            PhoneNumberType = "W",
            PhoneNumber = "5551234567",
            Extension = "101"
        };
        var contact = new SponsorContactInput
        {
            Name = "Jane Doe",
            PhoneNumberType = "M",
            PhoneNumber = "5559876543",
            Extension = null,
            Email = "jane@example.com"
        };
        var input = SponsorInputFactory.Create(
            logo: logo,
            businessStreet: "123 Main St",
            businessUnit: "Suite 4",
            businessCity: "Boston",
            businessState: "MA",
            businessPostalCode: "02108",
            businessEmailAddress: "info@example.com",
            phoneNumbers: [phoneNumber],
            contact: contact);
        var request = new CreateSponsorRequest { Sponsor = input };
        var ct = TestContext.Current.CancellationToken;
        var createdSponsor = CreatedSponsorFactory.Create();

        CreateSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateSponsorCommand>(), ct))
            .Callback<CreateSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(createdSponsor);

        var endpoint = Factory.Create<CreateSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.Logo.ShouldNotBeNull();
        capturedCommand.Logo.Container.ShouldBe(logo.Container);
        capturedCommand.Logo.Path.ShouldBe(logo.Path);
        capturedCommand.Logo.ContentType.ShouldBe(logo.ContentType);
        capturedCommand.Logo.SizeInBytes.ShouldBe(logo.SizeInBytes);

        capturedCommand.BusinessStreet.ShouldBe(input.BusinessStreet);
        capturedCommand.BusinessUnit.ShouldBe(input.BusinessUnit);
        capturedCommand.BusinessCity.ShouldBe(input.BusinessCity);
        capturedCommand.BusinessState.ShouldNotBeNull();
        capturedCommand.BusinessState.Value.ShouldBe("MA");
        capturedCommand.BusinessPostalCode.ShouldBe(input.BusinessPostalCode);
        capturedCommand.BusinessEmailAddress.ShouldBe(input.BusinessEmailAddress);

        capturedCommand.PhoneNumbers.ShouldHaveSingleItem();
        var capturedPhoneNumber = capturedCommand.PhoneNumbers.Single();
        capturedPhoneNumber.Type.Value.ShouldBe(phoneNumber.PhoneNumberType);
        capturedPhoneNumber.Number.ShouldBe(phoneNumber.PhoneNumber);
        capturedPhoneNumber.Extension.ShouldBe(phoneNumber.Extension);

        capturedCommand.ContactName.ShouldBe(contact.Name);
        capturedCommand.ContactPhoneType.ShouldNotBeNull();
        capturedCommand.ContactPhoneType.Value.ShouldBe(contact.PhoneNumberType);
        capturedCommand.ContactPhoneNumber.ShouldBe(contact.PhoneNumber);
        capturedCommand.ContactPhoneExtension.ShouldBe(contact.Extension);
        capturedCommand.ContactEmail.ShouldBe(contact.Email);
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateSponsorCommand>(), ct))
            .ReturnsAsync(SponsorErrors.SlugAlreadyExists("storm-products-inc"));

        var endpoint = Factory.Create<CreateSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateSponsorCommand>(), ct))
            .ReturnsAsync(SponsorErrors.NameRequired);

        var endpoint = Factory.Create<CreateSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /sponsors")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderSponsorsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateSponsorEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("sponsors"), "should be under the /sponsors path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}

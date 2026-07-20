using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Sponsors.EditSponsor;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Sponsors.EditSponsor;

[UnitTest]
[Component("Sponsors")]
public sealed class EditSponsorEndpointTests
{
    private const string ValidSponsorId = "01000000000000000000000001";

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when the edit succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenEditSucceeds()
    {
        // Arrange
        var input = EditSponsorInputFactory.Create(name: "My Sponsor");
        var request = new Neba.Api.Contracts.Sponsors.EditSponsor.EditSponsorRequest { Id = ValidSponsorId, Sponsor = input };
        var ct = TestContext.Current.CancellationToken;

        EditSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditSponsorCommand>(), ct))
            .Callback<EditSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.SponsorId.ShouldBe(new SponsorId(ValidSponsorId));
        capturedCommand.Name.ShouldBe(input.Name);
        capturedCommand.IsCurrentSponsor.ShouldBe(input.IsCurrentSponsor);
        capturedCommand.Priority.ShouldBe(input.Priority);
        capturedCommand.Tier.ShouldBe(SponsorTier.FromName(input.Tier));
        capturedCommand.Category.ShouldBe(SponsorCategory.FromName(input.Category));
        capturedCommand.WebsiteUrl.ShouldBe(input.WebsiteUrl);
        capturedCommand.TagPhrase.ShouldBe(input.TagPhrase);
        capturedCommand.Description.ShouldBe(input.Description);
        capturedCommand.LiveReadText.ShouldBe(input.LiveReadText);
        capturedCommand.PromotionalNotes.ShouldBe(input.PromotionalNotes);
        capturedCommand.FacebookUrl.ShouldBe(input.FacebookUrl);
        capturedCommand.InstagramUrl.ShouldBe(input.InstagramUrl);
        capturedCommand.BusinessStreet.ShouldBe(input.BusinessStreet);
        capturedCommand.BusinessUnit.ShouldBe(input.BusinessUnit);
        capturedCommand.BusinessCity.ShouldBe(input.BusinessCity);
        capturedCommand.BusinessState.ShouldBeNull();
        capturedCommand.BusinessPostalCode.ShouldBe(input.BusinessPostalCode);
        capturedCommand.BusinessEmailAddress.ShouldBe(input.BusinessEmailAddress);
        capturedCommand.Logo.ShouldBeNull();
        capturedCommand.ContactName.ShouldBeNull();
        capturedCommand.ContactPhoneType.ShouldBeNull();
        capturedCommand.ContactPhoneNumber.ShouldBeNull();
        capturedCommand.ContactPhoneExtension.ShouldBeNull();
        capturedCommand.ContactEmail.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map a supplied logo, business state, phone numbers, and contact onto the command")]
    public async Task HandleAsync_ShouldMapLogoBusinessStatePhoneNumbersAndContact_WhenSupplied()
    {
        // Arrange
        var logo = new SponsorLogoInput
        {
            Container = "sponsors",
            Path = "logos/01ARZ3-logo.png",
            ContentType = "image/png",
            SizeInBytes = 2048
        };
        var phoneNumber = new SponsorPhoneNumberInput
        {
            PhoneNumberType = "M",
            PhoneNumber = "5551234567",
            Extension = null
        };
        var contact = new SponsorContactInput
        {
            Name = "Jane Doe",
            PhoneNumberType = "H",
            PhoneNumber = "5559876543",
            Extension = "42",
            Email = "jane@example.com"
        };
        var input = EditSponsorInputFactory.Create(
            logo: logo,
            businessState: "MA",
            phoneNumbers: [phoneNumber],
            contact: contact);
        var request = new Neba.Api.Contracts.Sponsors.EditSponsor.EditSponsorRequest { Id = ValidSponsorId, Sponsor = input };
        var ct = TestContext.Current.CancellationToken;

        EditSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditSponsorCommand>(), ct))
            .Callback<EditSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();

        capturedCommand.Logo.ShouldNotBeNull();
        capturedCommand.Logo.Container.ShouldBe(logo.Container);
        capturedCommand.Logo.Path.ShouldBe(logo.Path);
        capturedCommand.Logo.ContentType.ShouldBe(logo.ContentType);
        capturedCommand.Logo.SizeInBytes.ShouldBe(logo.SizeInBytes);

        capturedCommand.BusinessState.ShouldBe(Neba.Api.Contacts.Domain.UsState.FromValue("MA"));

        capturedCommand.PhoneNumbers.ShouldHaveSingleItem();
        var capturedPhoneNumber = capturedCommand.PhoneNumbers.Single();
        capturedPhoneNumber.Type.ShouldBe(Neba.Api.Contracts.Contact.PhoneNumberType.FromValue(phoneNumber.PhoneNumberType));
        capturedPhoneNumber.Number.ShouldBe(phoneNumber.PhoneNumber);
        capturedPhoneNumber.Extension.ShouldBeNull();

        capturedCommand.ContactName.ShouldBe(contact.Name);
        capturedCommand.ContactPhoneType.ShouldBe(Neba.Api.Contracts.Contact.PhoneNumberType.FromValue(contact.PhoneNumberType));
        capturedCommand.ContactPhoneNumber.ShouldBe(contact.PhoneNumber);
        capturedCommand.ContactPhoneExtension.ShouldBe(contact.Extension);
        capturedCommand.ContactEmail.ShouldBe(contact.Email);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = new Neba.Api.Contracts.Sponsors.EditSponsor.EditSponsorRequest { Id = ValidSponsorId, Sponsor = EditSponsorInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditSponsorCommand>(), ct))
            .ReturnsAsync(SponsorErrors.SponsorNotFound(ValidSponsorId));

        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new Neba.Api.Contracts.Sponsors.EditSponsor.EditSponsorRequest { Id = ValidSponsorId, Sponsor = EditSponsorInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditSponsorCommand>(), ct))
            .ReturnsAsync(Error.Conflict("Test.Conflict", "A conflict occurred."));

        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = new Neba.Api.Contracts.Sponsors.EditSponsor.EditSponsorRequest { Id = ValidSponsorId, Sponsor = EditSponsorInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditSponsorCommand>(), ct))
            .ReturnsAsync(SponsorErrors.NameRequired);

        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PUT route under /sponsors")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute_UnderSponsorsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditSponsorCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<EditSponsorEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("sponsors"), "should be under the /sponsors path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
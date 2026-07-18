using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contacts;
using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors.EditSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Sponsors.EditSponsor;

internal sealed class EditSponsorEndpoint(Messaging.ICommandHandler<EditSponsorCommand, Updated> commandHandler)
    : Endpoint<EditSponsorRequest>
{
    private readonly Messaging.ICommandHandler<EditSponsorCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<SponsorsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSponsor.PolicyName);

        Description(description => description
            .WithName("EditSponsor")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditSponsorRequest req, CancellationToken ct)
    {
        var input = req.Sponsor;

        var command = new EditSponsorCommand
        {
            SponsorId = new SponsorId(req.Id),
            Name = input.Name,
            IsCurrentSponsor = input.IsCurrentSponsor,
            Priority = input.Priority,
            Tier = SponsorTier.FromName(input.Tier),
            Category = SponsorCategory.FromName(input.Category),
            Logo = input.Logo is null
                ? null
                : new StoredFile
                {
                    Container = input.Logo.Container,
                    Path = input.Logo.Path,
                    ContentType = input.Logo.ContentType,
                    SizeInBytes = input.Logo.SizeInBytes
                },
            WebsiteUrl = input.WebsiteUrl,
            TagPhrase = input.TagPhrase,
            Description = input.Description,
            LiveReadText = input.LiveReadText,
            PromotionalNotes = input.PromotionalNotes,
            FacebookUrl = input.FacebookUrl,
            InstagramUrl = input.InstagramUrl,
            BusinessStreet = input.BusinessStreet,
            BusinessUnit = input.BusinessUnit,
            BusinessCity = input.BusinessCity,
            BusinessState = string.IsNullOrWhiteSpace(input.BusinessState)
                ? null
                : UsState.FromValue(input.BusinessState),
            BusinessPostalCode = input.BusinessPostalCode,
            BusinessEmailAddress = input.BusinessEmailAddress,
            PhoneNumbers = [.. input.PhoneNumbers.Select(p => new PhoneNumberInput
            {
                Type = PhoneNumberType.FromValue(p.PhoneNumberType),
                Number = p.PhoneNumber,
                Extension = p.Extension
            })],
            ContactName = input.Contact?.Name,
            ContactPhoneType = input.Contact is null
                ? null
                : PhoneNumberType.FromValue(input.Contact.PhoneNumberType),
            ContactPhoneNumber = input.Contact?.PhoneNumber,
            ContactPhoneExtension = input.Contact?.Extension,
            ContactEmail = input.Contact?.Email
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}

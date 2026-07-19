using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Sponsors.EditSponsor;
using Neba.Api.Features.Sponsors.Domain;

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
        var contact = SponsorCommandMapper.MapContact(input.Contact);

        var command = new EditSponsorCommand
        {
            SponsorId = new SponsorId(req.Id),
            Name = input.Name,
            IsCurrentSponsor = input.IsCurrentSponsor,
            Priority = input.Priority,
            Tier = SponsorTier.FromName(input.Tier),
            Category = SponsorCategory.FromName(input.Category),
            Logo = SponsorCommandMapper.MapLogo(input.Logo),
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
            BusinessState = SponsorCommandMapper.MapBusinessState(input.BusinessState),
            BusinessPostalCode = input.BusinessPostalCode,
            BusinessEmailAddress = input.BusinessEmailAddress,
            PhoneNumbers = SponsorCommandMapper.MapPhoneNumbers(input.PhoneNumbers),
            ContactName = contact.Name,
            ContactPhoneType = contact.PhoneType,
            ContactPhoneNumber = contact.PhoneNumber,
            ContactPhoneExtension = contact.PhoneExtension,
            ContactEmail = contact.Email
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

            await SponsorMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
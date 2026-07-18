using ErrorOr;

using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.Contact;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors;

/// <summary>
/// Builds sponsor value objects (business address, business email, phone numbers, sponsor contact)
/// from raw command fields. Shared by <see cref="CreateSponsor.CreateSponsorCommandHandler"/> and
/// <see cref="EditSponsor.EditSponsorCommandHandler"/> so the two handlers apply identical validation.
/// </summary>
internal static class SponsorFieldBuilder
{
    public static ErrorOr<Address?> BuildBusinessAddress(
        string? street, string? unit, string? city, UsState? state, string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            return (Address?)null;
        }

        ArgumentNullException.ThrowIfNull(state);

        var result = Address.Create(street, unit, city ?? string.Empty, state, postalCode ?? string.Empty);
        return result.IsError ? result.Errors : result.Value;
    }

    public static ErrorOr<EmailAddress?> BuildBusinessEmail(string? businessEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(businessEmailAddress))
        {
            return (EmailAddress?)null;
        }

        var result = EmailAddress.Create(businessEmailAddress);
        return result.IsError ? result.Errors : result.Value;
    }

    public static ErrorOr<IReadOnlyCollection<PhoneNumber>> BuildPhoneNumbers(
        IReadOnlyCollection<PhoneNumberInput> phoneNumbers)
    {
        var built = new List<PhoneNumber>(phoneNumbers.Count);

        foreach (var phoneNumber in phoneNumbers)
        {
            var result = PhoneNumber.CreateNorthAmerican(phoneNumber.Type, phoneNumber.Number, phoneNumber.Extension);
            if (result.IsError)
            {
                return result.Errors;
            }

            built.Add(result.Value);
        }

        return built;
    }

    // All-or-nothing per scoping decision: if any of Name/Phone/Email is supplied, all three must be.
    public static ErrorOr<ContactInfo?> BuildSponsorContact(
        string? contactName, PhoneNumberType? contactPhoneType, string? contactPhoneNumber,
        string? contactPhoneExtension, string? contactEmail)
    {
        var anySupplied = !string.IsNullOrWhiteSpace(contactName)
            || !string.IsNullOrWhiteSpace(contactPhoneNumber)
            || !string.IsNullOrWhiteSpace(contactEmail);

        if (!anySupplied)
        {
            return (ContactInfo?)null;
        }

        ArgumentNullException.ThrowIfNull(contactPhoneType);

        var phoneResult = PhoneNumber.CreateNorthAmerican(contactPhoneType, contactPhoneNumber ?? string.Empty, contactPhoneExtension);
        if (phoneResult.IsError)
        {
            return phoneResult.Errors;
        }

        var emailResult = EmailAddress.Create(contactEmail ?? string.Empty);
        return emailResult.IsError
            ? emailResult.Errors
            : new ContactInfo { Name = contactName ?? string.Empty, Phone = phoneResult.Value, Email = emailResult.Value };
    }
}

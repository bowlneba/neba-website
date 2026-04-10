using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Website.Server.Contact;

namespace Neba.Website.Server.BowlingCenters;

internal static class BowlingCenterMappingExtensions
{
    extension(BowlingCenterSummaryResponse response)
    {
        public BowlingCenterSummaryViewModel ToViewModel()
        {
            var selectedPhoneNumber = response.PhoneNumbers
                .SingleOrDefault(phone => phone.PhoneNumberType == "Work")
                ?.PhoneNumber
                ?? response.PhoneNumbers.FirstOrDefault()?.PhoneNumber
                ?? throw new InvalidOperationException(
                    $"Bowling center '{response.CertificationNumber}' has no phone numbers; this violates the domain invariant.");

            return new()
            {
                Name = response.Name,
                CertificationNumber = response.CertificationNumber,
                Street = response.Street,
                Unit = response.Unit,
                City = response.City,
                State = response.State,
                PostalCode = PostalCodeFormatter.FormatForDisplay(response.PostalCode),
                Latitude = response.Latitude,
                Longitude = response.Longitude,
                PhoneUri = new($"tel:{selectedPhoneNumber}"),
                PhoneDisplay = PhoneNumberFormatter.FormatForDisplay(selectedPhoneNumber),
                Website = Uri.TryCreate(response.Website, UriKind.Absolute, out var websiteUri)
                    && (string.Equals(websiteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(websiteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    ? websiteUri
                    : null
            };
        }
    }
}
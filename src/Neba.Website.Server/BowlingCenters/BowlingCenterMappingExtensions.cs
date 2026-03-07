using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Website.Server.Contact;

namespace Neba.Website.Server.BowlingCenters;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Static types should be used

internal static class BowlingCenterMappingExtensions
{
    extension(BowlingCenterSummaryResponse response)
    {
        public BowlingCenterSummaryViewModel ToViewModel()
        {
            var workPhoneNumber = response.PhoneNumbers.SingleOrDefault(phone => phone.PhoneNumberType == "Work");

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
                PhoneUri = new($"tel:{workPhoneNumber?.PhoneNumber ?? response.PhoneNumbers.First().PhoneNumber}"),
                PhoneDisplay = PhoneNumberFormatter.FormatForDisplay(workPhoneNumber?.PhoneNumber ?? response.PhoneNumbers.First().PhoneNumber)
            };
        }
    }
}
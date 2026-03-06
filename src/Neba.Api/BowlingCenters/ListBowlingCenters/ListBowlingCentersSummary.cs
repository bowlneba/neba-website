using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Contact;

namespace Neba.Api.BowlingCenters.ListBowlingCenters;

internal sealed class ListBowlingCentersSummary
    : Summary<ListBowlingCentersEndpoint>
{
    public ListBowlingCentersSummary()
    {
        Summary = "Lists all bowling centers.";
        Description = "Retrieves a summary list of all bowling centers, including contact and location details.";

        Response(200, "The list of bowling centers.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<BowlingCenterSummaryResponse>
            {
                Items =
                [
                    new BowlingCenterSummaryResponse
                    {
                        CertificationNumber = "NEBA-001",
                        Name = "Acme Lanes",
                        Status = "Open",
                        Street = "123 Main St",
                        Unit = null,
                        City = "Springfield",
                        State = "MA",
                        PostalCode = "01103",
                        Latitude = 42.1015,
                        Longitude = -72.5898,
                        PhoneNumbers =
                        [
                            new PhoneNumberResponse
                            {
                                PhoneNumberType = "Work",
                                PhoneNumber = "14135550100x5553",
                            }
                        ],
                    }
                ],
            });
    }
}

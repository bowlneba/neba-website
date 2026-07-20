using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.ReferenceData;

namespace Neba.Api.ReferenceData.ListPhoneNumberTypes;

internal sealed class ListPhoneNumberTypesSummary
    : Summary<ListPhoneNumberTypesEndpoint>
{
    public ListPhoneNumberTypesSummary()
    {
        Summary = "Lists all phone number types.";
        Description = "Retrieves the full list of phone number types (Home, Mobile, Work, Fax), for populating phone-type dropdowns.";

        Response(200, "The list of phone number types.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<PhoneNumberTypeResponse>
            {
                Items =
                [
                    new PhoneNumberTypeResponse { Name = "Mobile", Code = "M" }
                ],
            });
    }
}

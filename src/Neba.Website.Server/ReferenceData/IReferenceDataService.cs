using ErrorOr;

using Neba.Api.Contracts.ReferenceData;

namespace Neba.Website.Server.ReferenceData;

internal interface IReferenceDataService
{
    Task<ErrorOr<List<UsStateResponse>>> GetUsStatesAsync(CancellationToken ct = default);

    Task<ErrorOr<List<PhoneNumberTypeResponse>>> GetPhoneNumberTypesAsync(CancellationToken ct = default);
}

using System.Security.Claims;

using Neba.Api.Contracts.Security;

namespace Neba.Api.Security.Infrastructure.Authorization;

/// <summary>
/// Reads whether an admin explicitly denied a permission to the caller. Denials are stored on the user and
/// never travel in the JWT, so a rule that depends on one asks the store.
/// </summary>
internal interface IPermissionDenialReader
{
    Task<bool> IsDeniedAsync(ClaimsPrincipal caller, Permissions permission, CancellationToken cancellationToken);
}
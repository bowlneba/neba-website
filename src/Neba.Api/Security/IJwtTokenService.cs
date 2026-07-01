using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security;

internal interface IJwtTokenService
{
    TokenPair CreateTokenPair(ApplicationUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<Permissions> permissions);
}
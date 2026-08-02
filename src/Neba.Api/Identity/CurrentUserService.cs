using System.Security.Claims;

namespace Neba.Api.Identity;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private const string AnonymousActorId = "anonymous";

    public string ActorId
        => AmbientActorContext.ActorId
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? AnonymousActorId;

    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated
            ?? false;
}

internal interface ICurrentUserService
{
    /// <summary>
    /// The ambient actor set via <see cref="AmbientActorContext"/> if one is active, otherwise
    /// the authenticated user's NameIdentifier claim, or "anonymous" if neither is present.
    /// </summary>
    string ActorId { get; }

    bool IsAuthenticated { get; }
}
using System.Security.Claims;

namespace Neba.Api.Identity;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private const string AnonymousActorId = "anonymous";

    public string ActorId
        => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? AnonymousActorId;

    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated
            ?? false;
}

internal interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's NameIdentifier claim, or "anonymous" if unauthenticated
    /// </summary>
    string ActorId { get; }

    bool IsAuthenticated { get; }
}
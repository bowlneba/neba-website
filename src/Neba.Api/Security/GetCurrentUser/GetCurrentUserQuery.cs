using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Security.GetCurrentUser;

internal sealed record GetCurrentUserQuery
    : IQuery<ErrorOr<UserDto>>
{
    public required Ulid UserId { get; init; }
}
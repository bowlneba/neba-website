using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Security.GetUser;

internal sealed record GetUserQuery
    : IQuery<ErrorOr<UserDetailDto>>
{
    public required Ulid UserId { get; init; }
}

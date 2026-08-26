using Neba.Api.Messaging;

namespace Neba.Api.Security.ListUsers;

internal sealed record ListUsersQuery
    : IQuery<IReadOnlyCollection<UserSummaryDto>>;
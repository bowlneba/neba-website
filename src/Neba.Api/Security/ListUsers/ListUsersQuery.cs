using Neba.Api.Messaging;

namespace Neba.Api.Security.ListUsers;

internal sealed record ListUsersQuery
    : IQuery<PagedResult<UserSummaryDto>>, IPaginationQuery
{
    /// <inheritdoc />
    public int Page { get; init; }

    /// <inheritdoc />
    public int PageSize { get; init; }
}

using FastEndpoints;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersRequest
{
    [BindFrom("page")]
    public int Page { get; set; } = 1;

    [BindFrom("pageSize")]
    public int PageSize { get; set; } = 20;
}
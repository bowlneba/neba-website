using Neba.Api.Messaging;

namespace Neba.Api.Database;

internal static class PaginationQueryableExtensions
{
    extension<T>(IQueryable<T> source)
    {
        public IQueryable<T> ApplyPagination(IPaginationQuery pagination)
            => source
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize);
    }
}
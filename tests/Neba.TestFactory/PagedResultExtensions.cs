using Neba.Api.Messaging;

namespace Neba.TestFactory;

public static class PagedResultExtensions
{
    extension<T>(IReadOnlyCollection<T> items)
    {
        public PagedResult<T> WithTotalItems(int totalItems)
            => new(items, totalItems);
    }
}

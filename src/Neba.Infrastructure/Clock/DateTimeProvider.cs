using Neba.Application.Clock;

namespace Neba.Infrastructure.Clock;

internal sealed class DateTimeProvider
    : IDateTimeProvider
{
    public DateOnly Today
        => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset UtcNow
        => DateTimeOffset.UtcNow;
}
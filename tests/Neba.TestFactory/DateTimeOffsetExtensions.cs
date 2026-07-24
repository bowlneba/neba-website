namespace Neba.TestFactory;

internal static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset value)
    {
        /// <summary>
        /// Truncates to microsecond precision, matching PostgreSQL's <c>timestamptz</c> storage precision,
        /// so values compared after a database round-trip match exactly.
        /// </summary>
        public DateTimeOffset TruncateToMicroseconds()
            => new(value.Ticks - (value.Ticks % (TimeSpan.TicksPerMillisecond / 1000)), value.Offset);
    }
}
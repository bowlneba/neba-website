namespace Neba.Website.Server.Time;

internal static class ClientTimeZoneServiceExtensions
{
    extension(IClientTimeZoneService service)
    {
        /// <summary>
        /// Converts <paramref name="utc"/> to the viewer's local time zone, or returns <see langword="null"/> when it has no value.
        /// </summary>
        public async Task<DateTimeOffset?> ToLocalOrNullAsync(DateTimeOffset? utc)
            => utc is { } value ? await service.ToLocalAsync(value) : null;
    }
}

// Shared browser timezone helper for converting UTC values to/from the viewer's local time.
export function getTimeZoneId() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

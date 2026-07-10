// Shared browser timezone helper for converting UTC values to the viewer's local time.
export function getTimezoneOffsetMinutes() {
    return new Date().getTimezoneOffset();
}
